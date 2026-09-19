using NUnit.Framework;
using UnityEditor;
using UValidation.Editor;

namespace UValidation.Showcase.Tests
{
    public class ScriptableObjectShowcaseTests
    {
        private const string k_ShowcaseFolderPath = "Assets/Showcase/ScriptableObjects";

        [TestCase("01 - Valid.asset", true)]
        [TestCase("02 - NotNull Failure.asset", false)]
        [TestCase("03 - NotEmpty Failure.asset", false)]
        [TestCase("04 - HasNoNulls Failure.asset", false)]
        [TestCase("05 - HasNoEmpties Failure.asset", false)]
        [TestCase("06 - IsValid Failure.asset", false)]
        [TestCase("07 - IValidatable Failure.asset", false)]
        [TestCase("08 - Combined Failures.asset", false)]
        public void ShowcaseAsset_HasExpectedValidationResult(string fileName, bool expectedResult)
        {
            var path = $"{k_ShowcaseFolderPath}/{fileName}";
            Assert.IsNotNull(
                AssetDatabase.LoadAssetAtPath<ScriptableObjectValidationShowcase>(path),
                $"The ScriptableObject showcase asset is missing at '{path}'.");
            Assert.AreEqual(
                expectedResult,
                ValidationHelper.IsScriptableObjectValidAtPath(path, false));
        }

        [Test]
        public void ShowcaseReferenceAsset_Exists()
        {
            var path = k_ShowcaseFolderPath + "/_Shared/Assigned Reference.asset";
            Assert.IsNotNull(
                AssetDatabase.LoadAssetAtPath<ScriptableObjectShowcaseReference>(path),
                $"The shared reference asset is missing at '{path}'.");
        }
    }
}
