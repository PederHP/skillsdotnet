using SkillsDotNet.Mcp;

namespace SkillsDotNet.Mcp.Tests;

public class SkillValidatorTests
{
    [Fact]
    public void Validate_ValidSkill_ReturnsNoErrors()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["name"] = "my-skill",
            ["description"] = "A valid skill"
        };

        var errors = SkillValidator.Validate(frontmatter, "my-skill");

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_MissingName_ReturnsError()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["description"] = "A valid skill"
        };

        var errors = SkillValidator.Validate(frontmatter);

        Assert.Contains(errors, e => e.Contains("'name' is required"));
    }

    [Fact]
    public void Validate_MissingDescription_ReturnsError()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["name"] = "my-skill"
        };

        var errors = SkillValidator.Validate(frontmatter);

        Assert.Contains(errors, e => e.Contains("'description' is required"));
    }

    [Fact]
    public void Validate_NameTooLong_ReturnsError()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["name"] = new string('a', 65),
            ["description"] = "A skill"
        };

        var errors = SkillValidator.Validate(frontmatter);

        Assert.Contains(errors, e => e.Contains("at most 64 characters"));
    }

    [Fact]
    public void Validate_NameWithUppercase_ReturnsError()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["name"] = "My-Skill",
            ["description"] = "A skill"
        };

        var errors = SkillValidator.Validate(frontmatter);

        Assert.Contains(errors, e => e.Contains("lowercase"));
    }

    [Fact]
    public void Validate_NameStartsWithHyphen_ReturnsError()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["name"] = "-my-skill",
            ["description"] = "A skill"
        };

        var errors = SkillValidator.Validate(frontmatter);

        Assert.Contains(errors, e => e.Contains("start or end with a hyphen"));
    }

    [Fact]
    public void Validate_NameEndsWithHyphen_ReturnsError()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["name"] = "my-skill-",
            ["description"] = "A skill"
        };

        var errors = SkillValidator.Validate(frontmatter);

        Assert.Contains(errors, e => e.Contains("start or end with a hyphen"));
    }

    [Fact]
    public void Validate_NameWithConsecutiveHyphens_ReturnsError()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["name"] = "my--skill",
            ["description"] = "A skill"
        };

        var errors = SkillValidator.Validate(frontmatter);

        Assert.Contains(errors, e => e.Contains("consecutive hyphens"));
    }

    [Fact]
    public void Validate_NameWithInvalidCharacters_ReturnsError()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["name"] = "my_skill",
            ["description"] = "A skill"
        };

        var errors = SkillValidator.Validate(frontmatter);

        Assert.Contains(errors, e => e.Contains("invalid character"));
    }

    [Fact]
    public void Validate_NameDoesNotMatchDirectory_ReturnsError()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["name"] = "my-skill",
            ["description"] = "A skill"
        };

        var errors = SkillValidator.Validate(frontmatter, "other-skill");

        Assert.Contains(errors, e => e.Contains("does not match directory name"));
    }

    [Fact]
    public void Validate_DescriptionTooLong_ReturnsError()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["name"] = "my-skill",
            ["description"] = new string('a', 1025)
        };

        var errors = SkillValidator.Validate(frontmatter);

        Assert.Contains(errors, e => e.Contains("at most 1024 characters"));
    }

    [Fact]
    public void Validate_UnknownField_ReturnsError()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["name"] = "my-skill",
            ["description"] = "A skill",
            ["unknown-field"] = "value"
        };

        var errors = SkillValidator.Validate(frontmatter);

        Assert.Contains(errors, e => e.Contains("Unknown field 'unknown-field'"));
    }

    [Fact]
    public void Validate_AllAllowedFields_ReturnsNoErrors()
    {
        var frontmatter = new Dictionary<string, object>
        {
            ["name"] = "my-skill",
            ["description"] = "A skill",
            ["license"] = "MIT",
            ["compatibility"] = "claude",
            ["allowed-tools"] = "tool1",
            ["metadata"] = new Dictionary<string, string> { ["key"] = "value" }
        };

        var errors = SkillValidator.Validate(frontmatter);

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateName_EmptyName_ReturnsError()
    {
        var errors = SkillValidator.ValidateName("");

        Assert.Contains(errors, e => e.Contains("must not be empty"));
    }
}
