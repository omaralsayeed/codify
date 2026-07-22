using System.ComponentModel.DataAnnotations;
using Codify.Domain.Enums;

namespace Codify.Application.DTOs.TestCases;

public class CreateTestCaseRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Input cannot be empty.")]
    public string Input { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "ExpectedOutput cannot be empty.")]
    public string ExpectedOutput { get; set; } = string.Empty;

    /// <summary>
    /// True = يظهر للطالب كـ example في صفحة المسألة.
    /// False = hidden — بيتستخدم في التقييم بس.
    /// </summary>
    public bool IsSample { get; set; }

    public TestCaseVisibility VisibilityMode { get; set; } = TestCaseVisibility.Hidden;

    public int OrderIndex { get; set; }
}
