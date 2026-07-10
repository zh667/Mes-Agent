using MesCopilot.Domain.Enums;

namespace MesCopilot.UnitTests.Domain.Enums;

public class UserRoleTests
{
    [Fact]
    public void UserRole_ShouldHaveAllRequiredValues()
    {
        UserRole[] values = Enum.GetValues<UserRole>();

        Assert.Contains(UserRole.Admin, values);
        Assert.Contains(UserRole.TeamLead, values);
        Assert.Contains(UserRole.QAInspector, values);
        Assert.Contains(UserRole.Operator, values);
    }

    [Theory]
    [InlineData(UserRole.Admin, 0)]
    [InlineData(UserRole.TeamLead, 1)]
    [InlineData(UserRole.QAInspector, 2)]
    [InlineData(UserRole.Operator, 3)]
    public void UserRole_ShouldHaveCorrectValues(UserRole role, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)role);
    }

    [Theory]
    [InlineData(UserRole.Admin, "Admin")]
    [InlineData(UserRole.TeamLead, "TeamLead")]
    [InlineData(UserRole.QAInspector, "QAInspector")]
    [InlineData(UserRole.Operator, "Operator")]
    public void UserRole_ShouldHaveCorrectStringRepresentation(UserRole role, string expected)
    {
        Assert.Equal(expected, role.ToString());
    }
}
