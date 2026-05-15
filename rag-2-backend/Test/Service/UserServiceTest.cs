#region

using System.IdentityModel.Tokens.Jwt;
using HttpExceptions.Exceptions;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using rag_2_backend.Infrastructure.Dao;
using rag_2_backend.Infrastructure.Database;
using rag_2_backend.Infrastructure.Database.Entity;
using rag_2_backend.Infrastructure.Module.Email;
using rag_2_backend.Infrastructure.Module.User;
using rag_2_backend.Infrastructure.Module.User.Dto;
using rag_2_backend.Infrastructure.Util;
using Xunit;

#endregion

namespace rag_2_backend.Test.Service;

public class UserServiceTest
{
    private readonly AccountConfirmationToken _accountToken;

    private readonly Mock<DatabaseContext> _contextMock = new(
        new DbContextOptionsBuilder<DatabaseContext>().Options
    );

    private readonly Mock<EmailService> _emailService = new(null!, null!);
    private readonly Mock<JwtSecurityTokenHandler> _jwtSecurityTokenHandlerMock = new();

    private readonly PasswordResetToken _passwordToken;
    private readonly SecondaryEmailToken _secondaryEmailToken;
    private readonly Mock<UserDao> _userMock;

    private readonly User _user = new("email@prz.edu.pl")
    {
        Id = 1,
        Name = "John",
        Password = HashUtil.HashPassword("password"),
        StudyCycleYearA = 2022,
        StudyCycleYearB = 2023,
        Course = new Course(),
        Group = "group"
    };

    private readonly UserService _userService;

    public UserServiceTest()
    {
        _accountToken = new AccountConfirmationToken
        {
            User = _user,
            Expiration = DateTime.Now.AddDays(7),
            Token = HashUtil.HashPassword("password")
        };
        _passwordToken = new PasswordResetToken
        {
            User = _user,
            Expiration = DateTime.Now.AddDays(7),
            Token = HashUtil.HashPassword("password")
        };
        _secondaryEmailToken = new SecondaryEmailToken
        {
            Token = "secondary-token",
            User = _user,
            Expiration = DateTime.Now.AddDays(1),
            PendingEmail = "private@gmail.com"
        };

        _userMock = new Mock<UserDao>(_contextMock.Object);
        Mock<RefreshTokenDao> refreshTokenDaoMock = new(_contextMock.Object);
        Mock<CourseDao> courseDaoMock = new(_contextMock.Object);
        _userMock.Setup(u => u.GetUserByIdOrThrow(It.IsAny<int>())).ReturnsAsync(_user);
        _userMock.Setup(u => u.GetUserByEmailOrThrow(It.IsAny<string>())).ReturnsAsync(_user);
        _userMock.Setup(u => u.GetUserByPrimaryOrSecondaryEmailOrThrow(It.IsAny<string>())).ReturnsAsync(_user);

        _userService = new UserService(_contextMock.Object, _emailService.Object,
            _userMock.Object, refreshTokenDaoMock.Object, courseDaoMock.Object);

        courseDaoMock.Setup(c => c.GetCourseByIdOrThrow(It.IsAny<int>()))!.ReturnsAsync(_user.Course);
        _contextMock.Setup(c => c.Users).Returns(() => new List<User> { _user }
            .AsQueryable().BuildMockDbSet().Object);
        _contextMock.Setup(c => c.AccountConfirmationTokens)
            .Returns(() => new List<AccountConfirmationToken> { _accountToken }.AsQueryable().BuildMockDbSet().Object);
        _contextMock.Setup(c => c.RefreshTokens)
            .Returns(() => new List<RefreshToken>().AsQueryable().BuildMockDbSet().Object);
        _contextMock.Setup(c => c.GameRecords)
            .Returns(() => new List<GameRecord>().AsQueryable().BuildMockDbSet().Object);
        _contextMock.Setup(c => c.PasswordResetTokens)
            .Returns(() => new List<PasswordResetToken> { _passwordToken }.AsQueryable().BuildMockDbSet().Object);
        _contextMock.Setup(c => c.SecondaryEmailTokens)
            .Returns(() => new List<SecondaryEmailToken> { _secondaryEmailToken }
                .AsQueryable().BuildMockDbSet().Object);
        _emailService.Setup(e => e.SendConfirmationEmail(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _emailService.Setup(e => e.SendPasswordResetMail(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _emailService.Setup(e => e.SendSecondaryEmailConfirmationMail(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _jwtSecurityTokenHandlerMock.Setup(e => e.ReadToken(It.IsAny<string>())).Returns(() => new JwtSecurityToken());
    }

    [Fact]
    public async Task ShouldRegisterUser()
    {
        await _userService.RegisterUser(new UserRequest
            {
                Email = "email1@prz.edu.pl", Password = "pass", StudyCycleYearA = 2022, StudyCycleYearB = 2023,
                Name = "John"
            }
        );

        _contextMock.Verify(c => c.Users.AddAsync(It.IsAny<User>(), CancellationToken.None), Times.Once);
        _contextMock.Verify(
            c => c.AccountConfirmationTokens.AddAsync(It.IsAny<AccountConfirmationToken>(), CancellationToken.None),
            Times.Once);
        _emailService.Verify(e => e.SendConfirmationEmail(It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        await Assert.ThrowsAsync<BadRequestException>(
            () => _userService.RegisterUser(new UserRequest
                {
                    Email = "email1@stud.prz.edu.pl", Password = "pass", StudyCycleYearA = 2020, StudyCycleYearB = 2023,
                    Name = "John"
                }
            )
        );
    }

    [Fact]
    public async Task ShouldResendConfirmationMail()
    {
        await _userService.ResendConfirmationEmail("email@prz.edu.pl");

        _contextMock.Verify(
            c => c.AccountConfirmationTokens.AddAsync(It.IsAny<AccountConfirmationToken>(), CancellationToken.None),
            Times.Once);
        _emailService.Verify(e => e.SendConfirmationEmail(It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        _user.Confirmed = true;
        await Assert.ThrowsAsync<BadRequestException>(() => _userService.ResendConfirmationEmail("email@prz.edu.pl"));
    }

    [Fact]
    public async Task ShouldConfirmAccount()
    {
        await _userService.ConfirmAccount(_accountToken.Token);
        _contextMock.Verify(c => c.AccountConfirmationTokens.Remove(It.IsAny<AccountConfirmationToken>()), Times.Once);

        await Assert.ThrowsAsync<BadRequestException>(() => _userService.ConfirmAccount("token")); //wrong token
        _accountToken.Expiration = DateTime.Now.AddDays(-7);
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _userService.ConfirmAccount(_accountToken.Token)); //invalid date
    }

    [Fact]
    public async Task ShouldRequestPasswordReset()
    {
        await _userService.RequestPasswordReset("email@prz.edu.pl");

        _contextMock.Verify(c => c.PasswordResetTokens.AddAsync(It.IsAny<PasswordResetToken>(), CancellationToken.None),
            Times.Once);
        _emailService.Verify(e => e.SendPasswordResetMail(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ShouldResetPassword()
    {
        await _userService.ResetPassword(_passwordToken.Token, "pass");
        _contextMock.Verify(c => c.PasswordResetTokens.Remove(It.IsAny<PasswordResetToken>()), Times.Once);

        await Assert.ThrowsAsync<BadRequestException>(() => _userService.ResetPassword("token1", "pass")); //wrong token
        _passwordToken.Expiration = DateTime.Now.AddDays(-7);
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _userService.ResetPassword(_passwordToken.Token, "pass")); //invalid date
    }

    [Fact]
    public async Task ShouldChangePassword()
    {
        await _userService.ChangePassword("email@prz.edu.pl", "password", "pass2");

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _userService.ChangePassword("email@prz.edu.pl", "pa4ss2", "pas2"));
    }

    [Fact]
    public async Task ShouldDeleteAccount()
    {
        await _userService.DeleteAccount("email@prz.edu.pl");

        _contextMock.Verify(c => c.Users.Remove(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task ShouldSetSecondaryEmail()
    {
        _user.Confirmed = true;

        await _userService.SetSecondaryEmail("private@gmail.com", "email@prz.edu.pl");

        _contextMock.Verify(
            c => c.SecondaryEmailTokens.AddAsync(It.IsAny<SecondaryEmailToken>(), CancellationToken.None),
            Times.Once);
        _emailService.Verify(
            e => e.SendSecondaryEmailConfirmationMail("private@gmail.com", It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ShouldNotSetSecondaryEmail_WhenUniversityDomain()
    {
        _user.Confirmed = true;

        await Assert.ThrowsAsync<BadRequestException>(
            () => _userService.SetSecondaryEmail("private@stud.prz.edu.pl", "email@prz.edu.pl")
        );
        await Assert.ThrowsAsync<BadRequestException>(
            () => _userService.SetSecondaryEmail("private@prz.edu.pl", "email@prz.edu.pl")
        );
    }

    [Fact]
    public async Task ShouldNotSetSecondaryEmail_WhenSameAsConfirmed()
    {
        _user.Confirmed = true;
        _user.SecondaryEmail = "already@gmail.com";

        await Assert.ThrowsAsync<BadRequestException>(
            () => _userService.SetSecondaryEmail("already@gmail.com", "email@prz.edu.pl")
        );

        _user.SecondaryEmail = null;
    }

    [Fact]
    public async Task ShouldNotSetSecondaryEmail_WhenEmailTakenByAnotherUser()
    {
        _user.Confirmed = true;
        var otherUser = new User("other@prz.edu.pl")
        {
            Name = "Other",
            Password = "x",
            SecondaryEmail = "taken@gmail.com"
        };
        _contextMock.Setup(c => c.Users).Returns(() =>
            new List<User> { _user, otherUser }.AsQueryable().BuildMockDbSet().Object);

        await Assert.ThrowsAsync<BadRequestException>(
            () => _userService.SetSecondaryEmail("taken@gmail.com", "email@prz.edu.pl")
        );

        _contextMock.Setup(c => c.Users).Returns(() =>
            new List<User> { _user }.AsQueryable().BuildMockDbSet().Object);
    }

    [Fact]
    public async Task ShouldConfirmSecondaryEmail()
    {
        await _userService.ConfirmSecondaryEmail(_secondaryEmailToken.Token);

        Assert.Equal("private@gmail.com", _user.SecondaryEmail);
        _contextMock.Verify(c => c.SecondaryEmailTokens.Remove(It.IsAny<SecondaryEmailToken>()), Times.Once);

        _user.SecondaryEmail = null;
    }

    [Fact]
    public async Task ShouldNotConfirmSecondaryEmail_WhenTokenInvalid()
    {
        await Assert.ThrowsAsync<BadRequestException>(
            () => _userService.ConfirmSecondaryEmail("wrong-token")
        );
    }

    [Fact]
    public async Task ShouldNotConfirmSecondaryEmail_WhenTokenExpired()
    {
        _secondaryEmailToken.Expiration = DateTime.Now.AddDays(-1);

        await Assert.ThrowsAsync<BadRequestException>(
            () => _userService.ConfirmSecondaryEmail(_secondaryEmailToken.Token)
        );

        _secondaryEmailToken.Expiration = DateTime.Now.AddDays(1);
    }
}