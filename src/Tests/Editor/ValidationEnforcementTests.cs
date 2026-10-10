using NUnit.Framework;
using UValidation.Editor;

namespace UValidation.Tests
{
    public class ValidationEnforcementTests
    {
        private UValidationProjectSettings m_Settings;
        private bool m_BlockInvalidSaves;
        private bool m_BlockPlayMode;
        private bool m_BlockEditorQuit;
        private bool m_FailBuild;

        [SetUp]
        public void SetUp()
        {
            m_Settings = UValidationProjectSettings.Instance;
            m_BlockInvalidSaves = m_Settings.BlockInvalidSaves;
            m_BlockPlayMode = m_Settings.BlockPlayMode;
            m_BlockEditorQuit = m_Settings.BlockEditorQuit;
            m_FailBuild = m_Settings.FailBuild;

            SetAllPolicies(false);
        }

        [TearDown]
        public void TearDown()
        {
            m_Settings.BlockInvalidSaves = m_BlockInvalidSaves;
            m_Settings.BlockPlayMode = m_BlockPlayMode;
            m_Settings.BlockEditorQuit = m_BlockEditorQuit;
            m_Settings.FailBuild = m_FailBuild;
        }

        [Test]
        public void ProjectPolicies_ControlTheirMatchingEnforcementRule()
        {
            m_Settings.BlockInvalidSaves = true;
            Assert.IsTrue(ValidationEnforcement.BlockInvalidSaves);
            Assert.IsFalse(ValidationEnforcement.BlockPlayMode);
            Assert.IsFalse(ValidationEnforcement.BlockEditorQuit);
            Assert.IsFalse(ValidationEnforcement.FailBuild);

            SetAllPolicies(false);
            m_Settings.BlockPlayMode = true;
            Assert.IsFalse(ValidationEnforcement.BlockInvalidSaves);
            Assert.IsTrue(ValidationEnforcement.BlockPlayMode);
            Assert.IsFalse(ValidationEnforcement.BlockEditorQuit);
            Assert.IsFalse(ValidationEnforcement.FailBuild);

            SetAllPolicies(false);
            m_Settings.BlockEditorQuit = true;
            Assert.IsFalse(ValidationEnforcement.BlockInvalidSaves);
            Assert.IsFalse(ValidationEnforcement.BlockPlayMode);
            Assert.IsTrue(ValidationEnforcement.BlockEditorQuit);
            Assert.IsFalse(ValidationEnforcement.FailBuild);

            SetAllPolicies(false);
            m_Settings.FailBuild = true;
            Assert.IsFalse(ValidationEnforcement.BlockInvalidSaves);
            Assert.IsFalse(ValidationEnforcement.BlockPlayMode);
            Assert.IsFalse(ValidationEnforcement.BlockEditorQuit);
            Assert.IsTrue(ValidationEnforcement.FailBuild);
        }

        private void SetAllPolicies(bool enabled)
        {
            m_Settings.BlockInvalidSaves = enabled;
            m_Settings.BlockPlayMode = enabled;
            m_Settings.BlockEditorQuit = enabled;
            m_Settings.FailBuild = enabled;
        }
    }
}
