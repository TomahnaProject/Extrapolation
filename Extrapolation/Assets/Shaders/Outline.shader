Shader "Unlit/Outline"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _OutlineWidth("Outline width", Range(-0.01,0.01)) = 0.002
        _OutlineConstantWidthDistance("Distance under which outline has constant width", Range(0,100)) = 5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Offset 1, 1

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            half _OutlineWidth;
            half _OutlineConstantWidthDistance;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                UNITY_FOG_COORDS(0)
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                half3 norm = mul(UNITY_MATRIX_V, normalize(mul((half3x3)unity_ObjectToWorld, float4(v.normal, 1.0)))).xyz;
                half2 offset = TransformViewToProjection(norm.xy);
                float distance = length(WorldSpaceViewDir(v.vertex));
                distance = clamp(distance, 0, _OutlineConstantWidthDistance);
                o.vertex.xy += offset * distance * _OutlineWidth;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 _Color;
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _Color;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
