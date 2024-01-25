using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Myst3;
using UnityEngine;

namespace Myst3
{
    public class ADGameFileDescription
    {
        /// <summary>Name of the described file.</summary>
        public string fileName;
        /// <summary>Optional. Not used during detection, only by engines.</summary>
        public ushort fileType;
        /// <summary>MD5 of (the beginning of) the described file. Optional. Set to NULL to ignore.</summary>
        public string md5;
        /// <summary>Size of the described file. Set to -1 to ignore.</summary>
        public long fileSize;

        public ADGameFileDescription(string fileName, ushort fileType, string md5, long fileSize)
        {
            this.fileName = fileName;
            this.fileType = fileType;
            this.md5 = md5;
            this.fileSize = fileSize;
        }
    };
    public class ADGameDescription
    {
        public string gameId;
        public string extra;
        public ADGameFileDescription[] filesDescriptions;
        public Language language;
        public Platform platform;
        public ADGameFlags flags;
        public string guiOptions;
    };

    [Flags]
    public enum ADGameFlags
    {
        ADGF_NO_FLAGS =  0, /// No flags.
        ADGF_TAILMD5 = 1 << 16, /// Calculate the MD5 for this entry from the end of the file.
        ADGF_AUTOGENTARGET = 1 << 17, /// Automatically generate gameid from @ref ADGameDescription::extra.
        ADGF_UNSTABLE = 1 << 18, /// Flag to designate not yet officially supported games that are not fit for public testing.
        ADGF_TESTING = 1 << 19, /// Flag to designate not yet officially supported games that are fit for public testing.
        ADGF_PIRATED = 1 << 20, /// Flag to designate well-known pirated versions with cracks.
        ADGF_UNSUPPORTED = 1 << 21, /* Flag to mark certain versions (like badly protected full games as demos) not to be run for various reasons.
                                       A custom message can be provided in the @ref ADGameDescription::extra field. */
        ADGF_WARNING = 1 << 22, /* Flag to mark certain versions to show confirmation warning before proceeding.
                                   A custom message should be provided in the @ref ADGameDescription::extra field. */
        ADGF_ADDENGLISH = 1 << 23, /// Always add English as a language option.
        ADGF_MACRESFORK = 1 << 24, /// Calculate the MD5 for this entry from the resource fork.
        ADGF_USEEXTRAASTITLE = 1 << 25, /// Use <see cref="ADGameDescription.extra"/> as the main game title, not gameid.
        ADGF_DROPLANGUAGE = 1 << 26, /// Do not add language to gameid.
        ADGF_DROPPLATFORM = 1 << 27, /// Do not add platform to gameid.
        ADGF_CD = 1 << 28, /// Add "-cd" to gameid.
        ADGF_DVD = 1 << 29, /// Add "-dvd" to gameid.
        ADGF_DEMO = 1 << 30, /// Add "-demo" to gameid.
        ADGF_REMASTERED = 1 << 31 /// Add "-remastered' to gameid.
    };

    public class Myst3MetaEngineDetection
    {
        readonly Myst3GameDescription[] _gameDescriptions;

        public Myst3MetaEngineDetection()
        {
            _gameDescriptions = new Myst3GameDescription[]
            {
                // Initial US release (English only) v1.0
                CreateEntry(Language.EN_ANY, "ENGLISH.m3t",  "19dcba1074f235ec2119313242d891de", null, GameLocalizationType.kLocMonolingual),

                // Initial US release (English only) v1.22
                CreateEntry(Language.EN_ANY, "ENGLISH.m3t",  "3ca92b097c4319a2ace7fd6e911d6b0f", null, GameLocalizationType.kLocMonolingual),

                // European releases (Country language + English) (1.2)
                CreateEntry(Language.NL_NLD, "DUTCH.m3u",    "0e8019cfaeb58c2de00ac114cf122220", null, GameLocalizationType.kLocMulti2),
                CreateEntry(Language.FR_FRA, "FRENCH.m3u",   "3a7e270c686806dfc31c2091e09c03ec", null, GameLocalizationType.kLocMulti2),
                CreateEntry(Language.DE_DEU, "GERMAN.m3u",   "1b2fa162a951fa4ed65617dd3f0c8a53", null, GameLocalizationType.kLocMulti2), // #1323, andrews05
                CreateEntry(Language.IT_ITA, "ITALIAN.m3u",  "906645a87ac1cbbd2b88c277c2b4fda2", null, GameLocalizationType.kLocMulti2), // #1323, andrews05
                CreateEntry(Language.ES_ESP, "SPANISH.m3u",  "28003569d9536cbdf6020aee8e9bcd15", null, GameLocalizationType.kLocMulti2), // #1323, goodoldgeorge
                CreateEntry(Language.PL_POL, "POLISH.m3u",   "8075e4e822e100ec79a5842a530dbe24", null, GameLocalizationType.kLocMulti2),

                // Russian release (Russian only) (1.2)
                CreateEntry(Language.RU_RUS, "ENGLISH.m3t",  "57d36d8610043fda554a0708d71d2681", null, GameLocalizationType.kLocMonolingual),

                // Hebrew release (Hebrew only) (1.2 - Patched using the patch CD)
                CreateEntry(Language.HE_ISR, "HEBREW.m3u",   "16fbbe420fed366249a8d44a759f966c", null, GameLocalizationType.kLocMonolingual), // #1348, BLooperZ

                // Japanese release (1.2)
                CreateEntry(Language.JA_JPN, "JAPANESE.m3u", "21bbd040bcfadd13b9dc84360c3de01d", null, GameLocalizationType.kLocMulti2),
                CreateEntry(Language.JA_JPN, "JAPANESE.m3u", "1e7c3156417978a1187fa6bc0e2cfafc", "Subtitles only", GameLocalizationType.kLocMulti2),

                // Multilingual CD release (1.21)
                CreateEntry(Language.EN_ANY, "ENGLISH.m3u",  "b62ca55aa17724cddbbcc78cba988337", null, GameLocalizationType.kLocMulti6),
                CreateEntry(Language.FR_FRA, "FRENCH.m3u",   "73519070cba1c7bea599adbddeae304f", null, GameLocalizationType.kLocMulti6),
                CreateEntry(Language.NL_NLD, "DUTCH.m3u",    "c4a8d8fb0eb3fecb9c435a8517bc1f9a", null, GameLocalizationType.kLocMulti6),
                CreateEntry(Language.DE_DEU, "GERMAN.m3u",   "5b3be343dd20f03ebdf16381b873f035", null, GameLocalizationType.kLocMulti6),
                CreateEntry(Language.IT_ITA, "ITALIAN.m3u",  "73db43aac3fe8671e2c4e227977fbb61", null, GameLocalizationType.kLocMulti6),
                CreateEntry(Language.ES_ESP, "SPANISH.m3u",  "55ceb165dad02211ef2d25946c3aac8e", null, GameLocalizationType.kLocMulti6),

                new()
                {
                    // Chinese (Simplified) CD release (1.22LC)
                    gameId = "myst3",
                    extra = "Missing game code", // Lacks OVER101.m3o file
                    filesDescriptions = new ADGameFileDescription[]
                    {
                        new("RSRC.m3r", 0, "a2c8ed69800f60bf5667e5c76a88e481", 1223862),
                        new("localized.m3t", 0, "3a9f299f8d061ce3d2862d985edb84e3", 2341588),
                        new("ENGLISHjp.m3t", 0, "19dcba1074f235ec2119313242d891de", 5658925),
                    },
                    language = Language.ZH_CNA,
                    platform = Platform.kPlatformWindows,
                    flags = ADGameFlags.ADGF_UNSUPPORTED,
                    // guiOptions = GUIO_NONE,
                    localizationType = GameLocalizationType.kLocMulti2 // CHS, English
                },

                // Japanese DVD release (1.24) (TRAC report #14298)
                CreateEntry(Language.JA_JPN, "JAPANESE.m3u", "5c18c9c124ff92d2b95ae5d128228f7b", "DVD", GameLocalizationType.kLocMulti2),

                // DVD releases (1.27)
                CreateDvdEntry(Language.EN_ANY, "ENGLISH.m3u",  "e200b416f43e70fee76148a80d195d5c", "DVD", GameLocalizationType.kLocMulti6),
                CreateDvdEntry(Language.FR_FRA, "FRENCH.m3u",   "5679ce65c5e9af8899835ef9af398f1a", "DVD", GameLocalizationType.kLocMulti6),
                CreateDvdEntry(Language.NL_NLD, "DUTCH.m3u",    "2997afdb4306c573153fdbb391ed2fff", "DVD", GameLocalizationType.kLocMulti6),
                CreateDvdEntry(Language.DE_DEU, "GERMAN.m3u",   "09f32e6ceb414463e8fc22ca1a9564d3", "DVD", GameLocalizationType.kLocMulti6),
                CreateDvdEntry(Language.IT_ITA, "ITALIAN.m3u",  "51fb02f6bf37dde811d7cde648365260", "DVD", GameLocalizationType.kLocMulti6),
                CreateDvdEntry(Language.ES_ESP, "SPANISH.m3u",  "e27e610fe8ce35223a3239ff170a85ec", "DVD", GameLocalizationType.kLocMulti6),

                // Myst 3 Xbox (PAL)
                CreateXboxEntry(Language.EN_ANY, "ENGLISHX.m3t", "c4d012ab02b8ca7d0c7e79f4dbd4e676"),
                CreateXboxEntry(Language.FR_FRA, "FRENCHX.m3t",  "94c9dcdec8794751e4d773776552751a"),
                CreateXboxEntry(Language.DE_DEU, "GERMANX.m3t",  "b9b66fcd5d4fbb95ac2d7157577991a5"),
                CreateXboxEntry(Language.IT_ITA, "ITALIANX.m3t", "3ca266019eba68123f6b7cae57cfc200"),
                CreateXboxEntry(Language.ES_ESP, "SPANISHX.m3t", "a9aca36ccf6709164249f3fb6b1ef148"),

                // Myst 3 Xbox (RUS)
                CreateXboxEntry(Language.RU_RUS, "ENGLISHX.m3t", "18cb50f5c5317586a128ca9eb3e03279"),

                new()
                {
                    // Myst 3 PS2 (NTSC-U/C)
                    gameId = "myst3",
                    extra = "PS2 version is not yet supported",
                    filesDescriptions = new ADGameFileDescription[]
                    {
                        new("RSRC.m3r", 0, "c60d37bfd3bb8b0bee143018447bb460", 346618151),
                    },
                    language = Language.UNK_LANG,
                    platform = Platform.kPlatformPS2,
                    flags = ADGameFlags.ADGF_UNSUPPORTED,
                    // guiOptions = GUIO_NONE,
                    localizationType = 0
                },

                new()
                {
                    // Myst 3 PS2 (PAL)
                    gameId = "myst3",
                    extra = "PS2 version is not yet supported",
                    filesDescriptions = new ADGameFileDescription[]
                    {
                        new("RSRC.m3r", 0, "f0e0c502f77157e6b5272686c661ea75", 91371793),
                    },
                    language = Language.UNK_LANG,
                    platform = Platform.kPlatformPS2,
                    flags = ADGameFlags.ADGF_UNSUPPORTED,
                    // guiOptions = GUIO_NONE,
                    localizationType = GameLocalizationType.kLocMonolingual
                }
            };
        }

        public Myst3GameDescription Detect(string path)
        {
            foreach (Myst3GameDescription description in _gameDescriptions)
            {
                foreach (ADGameFileDescription fileDesc in description.filesDescriptions)
                {
                    string[] result = Directory.GetFiles(path, fileDesc.fileName, SearchOption.AllDirectories);
                    if (result.Length != 0)
                    {
                        string filePath = result[0];
                        FileInfo fileInfo = new(filePath);
                        bool okLength = fileInfo.Length == fileDesc.fileSize;
                        bool okMd5 = GetFileMd5(filePath) == fileDesc.md5;

                        if (okLength && okMd5)
                            return description;
                    }
                }
            }
            // Debug.LogWarning("Game detection not supported yet.");
            return null;
        }

        static string GetFileMd5(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            byte[] arr = new byte[5000];
            stream.Read(arr, 0, arr.Length);
            return BitConverter.ToString(md5.ComputeHash(arr)).Replace("-", "").ToLowerInvariant();
        }

        Myst3GameDescription CreateEntry(Language lang, string langFile, string md5lang, string extra, GameLocalizationType flags)
        {
            return new Myst3GameDescription()
            {
                gameId = "myst3",
                extra = extra,
                filesDescriptions = new ADGameFileDescription[]
                {
                    new("RSRC.m3r", 0, "a2c8ed69800f60bf5667e5c76a88e481", 1223862),
                    new(langFile, 0, md5lang, -1),
                },
                language = lang,
                platform = Platform.kPlatformWindows,
                flags = ADGameFlags.ADGF_NO_FLAGS,
                // guiOptions = GUIO_NONE,
                localizationType = flags
            };
        }

        Myst3GameDescription CreateDvdEntry(Language lang, string langFile, string md5lang, string extra, GameLocalizationType flags)
        {
            return new Myst3GameDescription()
            {
                gameId = "myst3",
                extra = extra,
                filesDescriptions = new ADGameFileDescription[]
                {
                    new("RSRC.m3r", 0, "a2c8ed69800f60bf5667e5c76a88e481", 1223862),
                    new("ENGLISH.m3t", 0, "74726de866c0594d3f2a05ff754c973d", 3407120),
                    new(langFile, 0, md5lang, -1),
                },
                language = lang,
                platform = Platform.kPlatformWindows,
                flags = ADGameFlags.ADGF_NO_FLAGS,
                // guiOptions = GUIO_NONE,
                localizationType = flags
            };
        }

        Myst3GameDescription CreateXboxEntry(Language lang, string langFile, string md5lang)
        {
            return new Myst3GameDescription()
            {
                gameId = "myst3",
                extra = null,
                filesDescriptions = new ADGameFileDescription[]
                {
                    new("RSRC.m3r", 0, "3de23eb5a036a62819186105478f9dde", 1226192),
                    new(langFile, 0, md5lang, -1),
                },
                language = lang,
                platform = Platform.kPlatformXbox,
                flags = ADGameFlags.ADGF_UNSTABLE,
                // guiOptions = GUIO_NONE,
                localizationType = GameLocalizationType.kLocMulti6
            };
        }
        
    }
}
