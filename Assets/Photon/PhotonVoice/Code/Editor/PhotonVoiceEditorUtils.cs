﻿using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace Photon.Voice.Unity.Editor
{
    public static class PhotonVoiceEditorUtils
    {
        /// <summary>True if the ChatClient of the Photon Chat API is available. If so, the editor may (e.g.) show additional options in settings.</summary>
        public static bool HasChat
        {
            get
            {
                return Type.GetType("Photon.Chat.ChatClient, Assembly-CSharp") != null || Type.GetType("Photon.Chat.ChatClient, Assembly-CSharp-firstpass") != null || Type.GetType("Photon.Chat.ChatClient, PhotonChat") != null;
            }
        }

        /// <summary>True if the PhotonNetwork of the PUN is available. If so, the editor may (e.g.) show additional options in settings.</summary>
        public static bool HasPun
        {
            get
            {
                return Type.GetType("Photon.Pun.PhotonNetwork, Assembly-CSharp") != null || Type.GetType("Photon.Pun.PhotonNetwork, Assembly-CSharp-firstpass") != null || Type.GetType("Photon.Pun.PhotonNetwork, PhotonUnityNetworking") != null;
            }
        }
        
        [MenuItem("Window/Photon Voice/Remove PUN", true, 1)]
        private static bool RemovePunValidate()
        {
            #if PHOTON_UNITY_NETWORKING
            return true;
            #else
            return HasPun;
            #endif
        }

        [MenuItem("Window/Photon Voice/Remove PUN", false, 1)]
        private static void RemovePun()
        {
            DeleteDirectory("Assets/Photon/PhotonVoice/Demos/DemoProximityVoiceChat");
            DeleteDirectory("Assets/Photon/PhotonVoice/Demos/DemoVoicePun");
            DeleteDirectory("Assets/Photon/PhotonVoice/Code/PUN");
            DeleteDirectory("Assets/Photon/PhotonUnityNetworking");
            CleanUpPunDefineSymbols();
        }

        [MenuItem("Window/Photon Voice/Remove Photon Chat", true, 2)]
        private static bool RemovePhotonChatValidate()
        {
            return HasChat;
        }

        [MenuItem("Window/Photon Voice/Remove Photon Chat", false, 2)]
        private static void RemovePhotonChat()
        {
            DeleteDirectory("Assets/Photon/PhotonChat");
        }

        [MenuItem("Window/Photon Voice/Leave a review", false, 3)]
        private static void OpenAssetStorePage()
        {
            Application.OpenURL("https://assetstore.unity.com/packages/tools/audio/photon-voice-2-130518");
        }

        public static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                if (!FileUtil.DeleteFileOrDirectory(path))
                {
                    Debug.LogWarningFormat("Directory \"{0}\" not deleted.", path);
                }
                DeleteFile(string.Concat(path, ".meta"));
            }
            else
            {
                Debug.LogWarningFormat("Directory \"{0}\" does not exist.", path);
            }
        }

        public static void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                if (!FileUtil.DeleteFileOrDirectory(path))
                {
                    Debug.LogWarningFormat("File \"{0}\" not deleted.", path);
                }
            }
            else
            {
                Debug.LogWarningFormat("File \"{0}\" does not exist.", path);
            }
        }
        
        public static bool IsInTheSceneInPlayMode(GameObject go)
        {
            return Application.isPlaying && !IsPrefab(go);
        }

        public static void GetPhotonVoiceVersionsFromChangeLog(out string photonVoiceVersion, out string punChangelogVersion, out string photonVoiceApiVersion)
        {
            photonVoiceVersion = null;
            punChangelogVersion = null;
            photonVoiceApiVersion = null;
            string filePath = Path.Combine("Assets", "Photon", "PhotonVoice", "changes-voice.txt");
            const string guid = "63aaf8df43de62247af0bbdc549b31b5";
            if (!File.Exists(filePath))
            {
                filePath = AssetDatabase.GUIDToAssetPath(guid);
                Debug.LogFormat("Photon Voice 2's change log file was moved to this path \"{0}\"", filePath);
            }
            if (File.Exists(filePath))
            {
                try
                {
                    using (StreamReader file = new StreamReader(filePath))
                    {
                        while (!file.EndOfStream && (string.IsNullOrEmpty(photonVoiceVersion) || string.IsNullOrEmpty(punChangelogVersion) || string.IsNullOrEmpty(photonVoiceApiVersion)))
                        {
                            string line = file.ReadLine();
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                line = line.Trim();
                                if (line.StartsWith("v2."))
                                {
                                    if (!string.IsNullOrEmpty(photonVoiceVersion))
                                    {
                                        break;
                                    }
                                    photonVoiceVersion = line.TrimStart('v');
                                    continue;
                                }
                                string[] parts = line.Split(null);
                                if (line.StartsWith("PUN2: ") && parts.Length > 1)
                                {
                                    punChangelogVersion = parts[1];
                                    continue;
                                }
                                if (line.StartsWith("PhotonVoiceApi: ") && parts.Length > 2)
                                {
                                    photonVoiceApiVersion = string.Format("rev. {0}", parts[2]);
                                }
                            }
                        }
                    }
                }
                catch (IOException e)
                {
                    Debug.LogErrorFormat("There was an error reading the file \"{0}\": ", filePath);
                    Debug.LogError(e.Message);
                }
            }
            else
            {
                Debug.LogErrorFormat("Photon Voice 2's change log file not found (moved or deleted or not imported? or meta file changed) \"{0}\": ", filePath);
            }
            if (string.IsNullOrEmpty(photonVoiceVersion))
            {
                Debug.LogError("There was an error retrieving Photon Voice version from changelog.");
            }
            if (string.IsNullOrEmpty(punChangelogVersion))
            {
                Debug.LogError("There was an error retrieving PUN2 version from changelog.");
            }
            if (string.IsNullOrEmpty(photonVoiceApiVersion))
            {
                Debug.LogError("There was an error retrieving Photon Voice API version from changelog.");
            }
        }

        /// <summary>
		/// Check if a GameObject is a prefab asset or part of a prefab asset, as opposed to an instance in the scene hierarchy
		/// </summary>
		/// <returns><c>true</c>, if a prefab asset or part of it, <c>false</c> otherwise.</returns>
		/// <param name="go">The GameObject to check</param>
		public static bool IsPrefab(GameObject go)
		{
            #if UNITY_2018_3_OR_NEWER
            return UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(go) != null || EditorUtility.IsPersistent(go);
            #else
            return EditorUtility.IsPersistent(go);
			#endif
        }

        /// <summary>
        /// Removes PUN2's Script Define Symbols from project
        /// </summary>
        public static void CleanUpPunDefineSymbols()
        {
            foreach (BuildTarget target in Enum.GetValues(typeof(BuildTarget)))
            {
                BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);

                if (group == BuildTargetGroup.Unknown)
                {
                    continue;
                }

                var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group)
                    .Split(';')
                    .Select(d => d.Trim())
                    .ToList();

                List<string> newDefineSymbols = new List<string>();
                foreach (var symbol in defineSymbols)
                {
                    if ("PHOTON_UNITY_NETWORKING".Equals(symbol) || symbol.StartsWith("PUN_2_"))
                    {
                        continue;
                    }

                    newDefineSymbols.Add(symbol);
                }

                try
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", newDefineSymbols.ToArray()));
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("Could not set clean up PUN2's define symbols for build target: {0} group: {1}, {2}", target, group, e);
                }
            }
        }
    }
}