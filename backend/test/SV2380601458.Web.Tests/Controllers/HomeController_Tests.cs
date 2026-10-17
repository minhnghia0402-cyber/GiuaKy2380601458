using SV2380601458.Models.TokenAuth;
using SV2380601458.Web.Controllers;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace SV2380601458.Web.Tests.Controllers;

public class HomeController_Tests : SV2380601458WebTestBase
{
    [Fact]
    public async Task Index_Test()
    {
        await AuthenticateAsync(null, new AuthenticateModel
        {
            UserNameOrEmailAddress = "admin",
            Password = "123qwe"
        });

        //Act
        var response = await GetResponseAsStringAsync(
            GetUrl<HomeController>(nameof(HomeController.Index))
        );

        //Assert
        response.ShouldNotBeNullOrEmpty();
    }
}