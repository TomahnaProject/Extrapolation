using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEngine;

/// <summary>
/// Computes relative positions for nodes and points of interest as seen from those nodes.
/// </summary>
/// <remarks>
/// Solving node positions is an iterative algorithm, and it takes a while to run.
/// This class runs the computations in a background thread, and updates node positions in real time.
/// 
/// 3D positions for nodes and points of interest (PoI) are all expressed as <see cref="PointData"/>s.
/// The two are bound by <see cref="RelationData"/>s of node and PoI.
/// 
/// The scene is constantly re-centered, and the scene has a fixed width to prevent floating point rounding errors.
/// 
/// Algorithm originally written by Superbrain, adapted from the following research paper by Kangni-Laganière.
/// https://www.researchgate.net/publication/221111489_Orientation_and_Pose_recovery_from_Spherical_Panoramas
/// </remarks>
public class ThreadedSphericalPoseSolver : MonoBehaviour
{
    /// <summary>
    /// Maximum number of iterations for the solver. It may stop before that number if it can't lower the average error any further.
    /// </summary>
    public int maxIterations = 1000000;

    /// <summary>
    /// The max distance a point can be moved by each iteration.
    /// Lower is more stable but takes forever to converge.
    /// But since it computes in the background it's just better to keep it low anyway.
    /// </summary>
    public float stepSize = 0.001f;

    /// <summary>
    /// Whether the background computing thread is allowed to run computations.
    /// (The thread may still go to sleep if it's done computing. <seealso cref="Computing"/>)
    /// </summary>
    public bool Running
    {
        get => _running;
        set
        {
            if (_running == false && value == true)
                ResetSolver();
            _running = value;
        }
    }
    bool _running = false;

    /// <summary>
    /// How many milliseconds to sleep between iterations. Only for debugging. Leave at zero to keep it realtime.
    /// </summary>
    [Range(0, 500)]
    public int timeoutLengthMs;

    /// <summary>
    /// How wide the scene is, in meters.
    /// This prevents points from being knocked in outer orbit.
    /// </summary>
    [Range(1, 100)]
    public float sceneWidth = 100;

    /// <summary>
    /// Gives a rough indication of how fast the solver is running.
    /// </summary>
    public int numIterationsPerSec;

    /// <summary>
    /// Whether the background thread wants to compute data. If false, the thread went to sleep as it's done computing for now.
    /// </summary>
    public bool Computing { get; private set; }

    /// <summary>
    /// Current average error in degrees.
    /// </summary>
    public float CurError { get; private set; }

    /// <summary>
    /// How many solver iterations have run since the last modification to the dataset.
    /// </summary>
    public int IterationNumber { get; private set; }

    Thread _solverThread;

    /// <summary>
    /// All the relations we'll build the dataset from.
    /// </summary>
    readonly List<PoiOnNode> _relations = new();

    /// <summary>
    /// True when the background thread should stop and join back the main thread.
    /// </summary>
    bool quitThread = false;

    void Start()
    {
        _relations.Clear();
        // Start the solver thread, it will sleep until we feed it data.
        _solverThread = new Thread(new ThreadStart(SolverMainLoop));
        _solverThread.Start();
    }

    void OnDestroy()
    {
        Computing = false;
        IterationNumber = int.MaxValue;
        ClearData();
        quitThread = true;
        _solverThread.Join();
        _solverThread = null;
    }

    /// <summary>
    /// Remove a point of interest and all its related PoiOnNodes.
    /// </summary>
    /// <param name="poi">The point of interest to remove from the solver.</param>
    public void RemovePointOfInterest(PointOfInterest poi)
    {
        _relations.RemoveAll(pon => pon.Point == poi);
        ResetSolver();
    }

    /// <summary>
    /// Adds (or simply update the direction of) the specified POI on node.
    /// The positions of its linked node/POI will be updated accordingly as the solver is running.
    /// </summary>
    /// <param name="pon">The POI on node that represents the relation.</param>
    public void AddOrUpdatePoiOnNode(PoiOnNode pon)
    {
        if (!_relations.Contains(pon))
            _relations.Add(pon);
        ResetSolver();
    }

    /// <summary>
    /// Removes the given POI on node relation from the solver's process.
    /// </summary>
    /// <param name="pon">The POI on node that represents the relation.</param>
    public void RemovePoiOnNode(PoiOnNode pon)
    {
        _relations.Remove(pon);
        ResetSolver();
    }

    /// <summary>
    /// Clear all data from the solver. This will make the computing thread go to sleep.
    /// </summary>
    public void ClearData()
    {
        _dataset = null;
        _relations.Clear();
    }

    /// <summary>
    /// Adds the specified POIs on nodes.
    /// The positions of all linked nodes/POIs will be updated accordingly as the solver is running.
    /// </summary>
    /// <param name="poiOnNodes">All the relations to add to the solver.</param>
    public void AddAllPoiOnNode(List<PoiOnNode> poiOnNodes)
    {
        foreach (PoiOnNode pon in poiOnNodes)
        {
            if (!_relations.Contains(pon))
                _relations.Add(pon);
        }
        ResetSolver();
    }

    /// <summary>
    /// Get the point on line a that is closest to line b.
    /// Project everything onto a plane, so that the entirety of line b looks like a point.
    /// </summary>
    /// <param name="aOrigin">A point on line a.</param>
    /// <param name="aDir">The direction of line a.</param>
    /// <param name="bOrigin">A point on line b.</param>
    /// <param name="bDir">The direction of line b.</param>
    /// <returns>The point on line a closest to line b.</returns>
    static Vector3 ClosestPointLineLine(Vector3 aOrigin, Vector3 aDir, Vector3 bOrigin, Vector3 bDir)
    {
        Vector3 aOriginPrime = Vector3.ProjectOnPlane(aOrigin, bDir);
        Vector3 aDirectionPrime = Vector3.ProjectOnPlane(aDir, bDir);
        Vector3 bOriginPrime = Vector3.ProjectOnPlane(bOrigin, bDir);

        float t = Vector3.Dot(bOriginPrime - aOriginPrime, aDirectionPrime) / Vector3.Dot(aDirectionPrime, aDirectionPrime);
        return aOrigin + t * aDir;
    }

    /// <summary>
    /// Guess a somewhat valid 3D position for the given point given the relations it's involved in.
    /// </summary>
    /// <param name="point">The point to find a position for.</param>
    /// <param name="relations">All the relations we know about.</param>
    /// <returns>A <see cref="Vector3"/> if we can guess a position, or null if there aren't enough relations using this point.</returns>
    static Vector3? EstimateInitialPosition(PointData point, List<RelationData?> relations)
    {
        IEnumerator<RelationData?> relationsContainingPoint = relations
            .Where(r => r.Value.looker == point || r.Value.observed == point)
            .GetEnumerator();

        bool NextLineFromRelations(IEnumerator<RelationData?> relations, out Vector3 origin, out Vector3 direction)
        {
            while (relations.MoveNext())
            {
                RelationData? candidate = relations.Current;
                if (candidate.Value.observed == point)
                {
                    origin = candidate.Value.looker.position;
                    direction = candidate.Value.direction;
                }
                else
                {
                    origin = candidate.Value.observed.position;
                    direction = -candidate.Value.direction;
                }
                return true;
            }
            // No suitable relation found in iterator
            origin = direction = Vector3.zero;
            return false;
        }

        if (NextLineFromRelations(relationsContainingPoint, out Vector3 line_1_origin, out Vector3 line_1_dir) &&
            NextLineFromRelations(relationsContainingPoint, out Vector3 line_2_origin, out Vector3 line_2_dir))
        {
            // We have two lines - try to find an intersection.
            return ClosestPointLineLine(line_1_origin, line_1_dir, line_2_origin, line_2_dir);
        }
        // Can't even find two lines, so we can't guess a starting position.
        return null;
    }

    void ResetSolver()
    {
        // Let's recreate the whole dataset...
        // Since the transforms are sync'ed to a recent iteration on Update(), we can just recreate it from scratch,
        // and this will be enough to get the solver data in an acceptable configuration.
        SolverRunningDataset dataset = new();

        Dictionary<Transform, PointData> points = new();
        foreach (PoiOnNode pon in _relations)
        {
            if (!points.ContainsKey(pon.Point.transform))
            {
                points[pon.Point.transform] = new PointData()
                {
                    position = pon.Point.positionInitialized ? pon.Point.transform.position : pon.Point.transform.position + new Vector3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value),
                    nextOffset = Vector3.zero,
                    targetTransform = pon.Point.transform
                };
            }
            if (!points.ContainsKey(pon.Node.transform))
            {
                points[pon.Node.transform] = new PointData()
                {
                    position = pon.Node.positionInitialized ? pon.Node.transform.position : pon.Node.transform.position + new Vector3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value),
                    nextOffset = Vector3.zero,
                    targetTransform = pon.Node.transform
                };
            }
        }
        List<RelationData?> relations = _relations.Select(pon => (RelationData?)new RelationData()
        {
            direction = pon.Direction,
            looker = points[pon.Node.transform],
            observed = points[pon.Point.transform]
        }).ToList();

        // Make sure all the referenced points exist or filter out relations, if initialization isn't possible.
        List<RelationData?> validRelations = new();
        List<PointData> finalPoints = new();
        foreach (RelationData? relation in relations.Concat(relations).Concat(relations)) // Do it multiple times, to catch extra points.
        {
            if (!finalPoints.Contains(relation.Value.looker))
            {
                // Initialize the point with an estimated position, based on two relations
                Vector3? newPosition = EstimateInitialPosition(relation.Value.looker, relations);
                if (newPosition.HasValue)
                {
                    // relation.Value.looker.position = newPosition.Value;
                    finalPoints.Add(relation.Value.looker);
                }
                else
                {
                    // Rejected relation, because we don't have enough information.
                    continue;
                }
            }
            if (!finalPoints.Contains(relation.Value.observed))
            {
                // Initialize the point with an estimated position, based on two relations
                Vector3? newPosition = EstimateInitialPosition(relation.Value.observed, relations);
                if (newPosition.HasValue)
                {
                    // relation.Value.observed.position = newPosition.Value;
                    finalPoints.Add(relation.Value.observed);
                }
                else
                {
                    // Rejected relation, because we don't have enough information.
                    continue;
                }
            }
            if (!validRelations.Contains(relation))
                validRelations.Add(relation);
        }
        print($"Solver dataset rebuild: {validRelations.Count} relations, {finalPoints.Count} points.");

        foreach (PointData point in finalPoints)
        {
            // point.targetTransform.position = point.position;
            if (point.targetTransform.TryGetComponent(out NodeRenderer node))
                node.positionInitialized = true;
            else
                point.targetTransform.GetComponent<PointOfInterest>().positionInitialized = true;
        }
        
        dataset.relations = validRelations.Select(r => r.Value).ToArray();
        dataset.points = finalPoints.ToArray();

        // Replace the current dataset. This will discard the current solver iteration (which we don't really care about
        // since it's likely not too far away from what we currently have), and it will run on this set in the next iteration.
        _dataset = dataset;

        // Make sure to kickstart the solver if it's dozing.
        IterationNumber = 0;
        Computing = true;
    }

    void Update()
    {
        if (!Computing || !Running || IterationNumber > maxIterations || _dataset == null)
            return;

        // Copy positions back to points of interest/nodes.
        // Note that the solver thread may still be fiddling with the data, so we may have positions
        // from separate iterations. This is no big deal.
        foreach (PointData point in _dataset.points)
            point.targetTransform.localPosition = point.position;
    }

    #region Data and dataset

    /// <summary>
    /// The dataset the solver is always iterating on. It gets sync'ed to the main thread every now and then.
    /// </summary>
    class SolverRunningDataset
    {
        public RelationData[] relations;
        public PointData[] points;
    }
    /// <summary>
    /// Dataset in use for the next solver iteration.
    /// </summary>
    SolverRunningDataset _dataset;

    public class PointData
    {
        public Vector3 position;
        public Vector3 nextOffset;
        public int[] relationIds;
        public Transform targetTransform;
    }

    public struct RelationData
    {
        public PointData looker;
        public PointData observed;
        public Vector3 direction;

        /// <summary>
        /// Computes an error factor, comparing the computed position of the given points relative to our pitch/heading.
        /// </summary>
        /// <param name="looker">Where we're looking from (center of node).</param>
        /// <param name="observed">What we're looking at (our target point).</param>
        /// <returns>An error factor. Closer to zero is better.</returns>
        public readonly float GetError()
        {
            Vector3 calculated_vector = observed.position - looker.position;
            calculated_vector = calculated_vector.normalized;
            // Compare the angle between the unit vector that accurately represents our pitch/heading against the vector that was computed by the solver.
            float dot_product = Vector3.Dot(direction, calculated_vector);
            return Mathf.Pow(1 - dot_product, 2);
        }
    }

    #endregion

    #region Background thread stuff

    /// <summary>
    /// The gradient of (1-(dot(measured,(observed-looker))/norm(observed-looker))) ** 2 w.r.t looker.
    /// Symmetry: grad(measured, looker, observed) = -grad(-measured, observed, looker).
    /// I tried to make it a bit more compact and maybe faster.
    /// Can't really do much about the instability of the subtraction.
    /// </summary>
    /// <param name="direction">The true direction vector from <see cref="looker"/> to <see cref="observed"/>, computed from heading and pitch.</param>
    /// <param name="looker">Where we're looking from (center of node).</param>
    /// <param name="observed">What we're looking at (our point of interest).</param>
    /// <returns>An offset to apply to the points.</returns>
    static Vector3 Grad(Vector3 direction, Vector3 looker, Vector3 observed)
    {
        Vector3 d = observed - looker;
        float inv_norm = 1 / d.magnitude;
        float c = Vector3.Dot(direction, d) / Vector3.Dot(d, d);
        return 2 * (inv_norm - c) * (direction - c * d);
    }

    /// <summary>
    /// Compute the mean error of all given relations. This progressively diminishes as points converge.
    /// </summary>
    /// <param name="relations">Relations to compute error from.</param>
    /// <returns>Average error, lower is better.</returns>
    static float CalculateAverageError(ICollection<RelationData> relations)
    {
        float total = 0;
        foreach (RelationData relation in relations)
        {
            float error = relation.GetError();
            total += error;
        }
        return total / relations.Count;
    }

    /// <summary>
    /// Continually runs computations on the dataset.
    /// </summary>
    void SolverMainLoop()
    {
        Stopwatch watch = Stopwatch.StartNew();
        int lastIterationNumber = 0;
        // float previousError = float.PositiveInfinity;
        while (!quitThread)
        {
            // Copy the reference to the dataset, in case the main thread changes it while we're working.
            SolverRunningDataset dataset = _dataset;
            if (!Computing || !Running || IterationNumber > maxIterations || dataset == null)
            {
                Thread.Sleep(500);
                continue;
            }

            // First, re-center all points, then average the width of the whole scene to 100 meters. So things don't spiral out of proportions.
            if (dataset.points.Length > 0)
            {
                Bounds bounds = new(dataset.points[0].position, Vector3.zero);
                foreach (PointData point in dataset.points)
                    bounds.Encapsulate(point.position);
                foreach (PointData point in dataset.points)
                    point.position = (point.position - bounds.center) / bounds.size.magnitude * sceneWidth;
            }

            // Alas, there aren't many ways to avoid cache misses in this algorithm, so I don't think further optimizations will change much.

            // Compute the offset for each point in our relation. SIMD should help us here.
            foreach (ref RelationData relation in dataset.relations.AsSpan())
            {
                Vector3 offset = Grad(relation.direction, relation.looker.position, relation.observed.position) * stepSize;
                // (FUTURE: this would be a good time to multiply by the weight of the relation, when applicable.)
                // Store the offset for later use. We will merge it back into the point's position once all relations are done computing.
                relation.looker.nextOffset -= offset;
                relation.observed.nextOffset += offset;
            }

            // Merge offsets back into point positions.
            foreach (PointData point in dataset.points)
            {
                point.position += point.nextOffset;
                point.nextOffset = Vector3.zero;
            }

            int averageErrorUpdatesPerSecond = 10;
            int millisecondsBetweenErrorUpdates = (int)(1 / (float)averageErrorUpdatesPerSecond * 1000);
            if (watch.ElapsedMilliseconds > millisecondsBetweenErrorUpdates)
            {
                CurError = CalculateAverageError(dataset.relations);
                // if (previousError - CurError <= float.Epsilon)
                // {
                //     // Data doesn't get any more accurate, so we've reached a satisfying result. Stop processing.
                //     Computing = false;
                // }
                // previousError = CurError;

                int numIterations = IterationNumber - lastIterationNumber;
                numIterationsPerSec = (int)(numIterations / (millisecondsBetweenErrorUpdates / 1000.0f));
                lastIterationNumber = IterationNumber;

                // Relinquish time, just to free up resources a bit.
                Thread.Sleep(0);
                watch.Restart();
            }
            IterationNumber++;

            if (timeoutLengthMs > 0)
            {
                // Artificially sleep, so we can better see what's going on.
                Thread.Sleep(timeoutLengthMs);
            }
        }
    }

    #endregion
}
