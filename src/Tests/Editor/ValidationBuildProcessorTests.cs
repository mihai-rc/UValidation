using System.Text.RegularExpressions;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NUnit.Framework;
using UValidation.Editor;

namespace UValidation.Tests
{
    public class ValidationBuildProcessorTests
    {
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
        public void OnProcessScene_NullBuildReport_DoesNotEnforcePlayerBuildPolicy()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var gameObject = new GameObject(nameof(OnProcessScene_NullBuildReport_DoesNotEnforcePlayerBuildPolicy));
                SceneManager.MoveGameObjectToScene(gameObject, scene);
                gameObject.AddComponent<PrefabValidationProbe>();
                m_Settings.FailBuild = true;

                var processor = new ValidationBuildProcessor();

                Assert.DoesNotThrow(() => processor.OnProcessScene(scene, null));
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
                var gameObject = new GameObject(nameof(ProcessScene_PlayerBuild_EnforcesPlayerBuildPolicy));
                SceneManager.MoveGameObjectToScene(gameObject, scene);
                gameObject.AddComponent<PrefabValidationProbe>();
                m_Settings.FailBuild = true;

                LogAssert.Expect(
                    LogType.Error,
                    new Regex(@"(?s)\[Validation\] String is null or empty!.*Variable: m_Value"));
                Assert.Throws<BuildFailedException>(() => ValidationBuildProcessor.ProcessScene(scene, true));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
