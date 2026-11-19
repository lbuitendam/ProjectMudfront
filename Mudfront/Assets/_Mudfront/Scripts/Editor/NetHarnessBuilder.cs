using System.IO;
using FishNet.Managing;
using FishNet.Managing.Transporting;
using FishNet.Transporting.Tugboat;
using Mudfront.Economy;
using Mudfront.Net;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mudfront.EditorTools
{
    /// <summary>Generates the ECON-04 network harness scene and builds the Windows player used as the
    /// headless dedicated server (-batchmode -nographics) and as scripted clients.
    /// Batch: -executeMethod Mudfront.EditorTools.NetHarnessBuilder.BuildPlayer</summary>
    public static class NetHarnessBuilder
    {
        public const string ScenePath = "Assets/_Mudfront/Scenes/Net_Harness.unity";
        public const string PlayerPath = "../Builds/NetHarness/MudfrontNet.exe";

        [MenuItem("Mudfront/Net/Build Harness Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Network");
            var nm = go.AddComponent<NetworkManager>();
            var tug = go.AddComponent<Tugboat>();
            var tm = go.GetComponent<TransportManager>() ?? go.AddComponent<TransportManager>();
            var so = new SerializedObject(tm);
            var prop = so.FindProperty("Transport");
            if (prop != null) { prop.objectReferenceValue = tug; so.ApplyModifiedPropertiesWithoutUndo(); }
            var auth = go.AddComponent<AccountAuthenticator>();
            var server = go.AddComponent<EconomyServer>();
            server.Config = AssetDatabase.LoadAssetAtPath<EconomyConfig>(EconomyCatalogBuilder.ConfigPath);
            var boot = go.AddComponent<NetHarnessBootstrap>();
            boot.Network = nm; boot.Auth = auth; boot.Server = server;
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[NetHarness] scene saved " + ScenePath);
        }

        public static void BuildPlayer()
        {
            BuildScene();
            Directory.CreateDirectory(Path.GetDirectoryName(PlayerPath));
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = PlayerPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log($"[NetHarness] build {report.summary.result} errors={report.summary.totalErrors} size={report.summary.totalSize}");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        /// <summary>Prints a dev token for an account (dev authenticator only).</summary>
        public static string DevToken(string account, string secret) => AccountAuthenticator.IssueDevToken(account, secret);
    }
}
