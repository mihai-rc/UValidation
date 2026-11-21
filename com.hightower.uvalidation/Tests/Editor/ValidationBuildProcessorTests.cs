using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NUnit.Framework;
using UValidation.Editor;
using static UValidation.Tests.ValidationTestUtility;

namespace UValidation.Tests
{
    public class ValidationBuildProcessorTests
    {
        private const string k_InvalidObjectName = "InvalidBuildSceneObject";

        private UValidationProjectSettings m_Settings;
        private bool m_FailBuild;

        [SetUp]
        public void SetUp()
        {
            m_Settings = UValidationProjectSettings.Instance;
            m_FailBuild = m_Settings.FailBuild;
            m_Settings.FailBuild = false;
        }

        [TearDown]
        public void TearDown()
        {
            m_Settings.FailBuild = m_FailBuild;
        }

        [Test]
        public void OnProcessScene_OutsidePlayerBuild_DoesNotEnforcePlayerBuildPolicy()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                CreateInvalidObject(scene);
                m_Settings.FailBuild = true;
                var processor = new ValidationBuildProcessor();
                Assert.IsFalse(BuildPipeline.isBuildingPlayer, "The test runner must not be producing a player.");

                Assert.DoesNotThrow(() => processor.OnProcessScene(scene, null));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void ProcessScene_PlayerBuild_EnforcesPlayerBuildPolicy()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                CreateInvalidObject(scene);
                m_Settings.FailBuild = true;

                LogAssert.Expect(LogType.Error, CreateValidationErrorPattern(k_InvalidObjectName));
                Assert.Throws<BuildFailedException>(() => ValidationBuildProcessor.ProcessScene(scene, true));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void CreateInvalidObject(Scene scene)
        {
            var gameObject = new GameObject(k_InvalidObjectName);
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            gameObject.AddComponent<PrefabValidationProbe>();
        }
    }
}
