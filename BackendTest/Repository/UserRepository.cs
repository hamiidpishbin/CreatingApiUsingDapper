using System.Data;
using BackendTest.Data;
using BackendTest.Dtos;
using BackendTest.Models;
using Dapper;

namespace BackendTest.Repository;

public class UserRepository : IUserRepository
{
    private readonly string DefaultRoleId = "1";
    
    private readonly DapperContext _dapperContext;

    public UserRepository(DapperContext dapperContext)
    {
        _dapperContext = dapperContext;
    }
    
    public async Task<List<CreatedUserDto>> FindAllUsers()
    {
        var query = @"SELECT * FROM Users";
        
        using var connection = _dapperContext.CreateConnection();

        var users = await connection.QueryAsync<CreatedUserDto>(query);

        return users.ToList();
    }

    public async Task<CreatedUserDto> CreateUser(UserDto user)
    {
        var query = @"INSERT INTO Users (Username, Password) VALUES (@username, @password)" + @"SELECT CAST(SCOPE_IDENTITY() as int)";

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);

        var parameters = new DynamicParameters();
        parameters.Add("username", user.Username, DbType.String);
        parameters.Add("password", hashedPassword, DbType.String);
        
        using var connection = _dapperContext.CreateConnection();

        var createdUserId = await connection.QuerySingleAsync<int>(query, parameters);

        var createdUser = new CreatedUserDto()
        {
            Id = createdUserId,
            Username = user.Username
        };
        
        return createdUser;
    }
    
    public async Task<User> FindUserByUsername(string username)
    {
        var query = @"SELECT * FROM Users WHERE Username = @username";

        var parameters = new DynamicParameters();
        parameters.Add("username", username, DbType.String);
        
        using var connection = _dapperContext.CreateConnection();

        var user = await connection.QuerySingleOrDefaultAsync<User>(query, parameters);
        
        return user;
    }
    
    public async Task<User> FindUserById(int id)
    {
        var query = @"SELECT * FROM Users WHERE Id = @userId";

        using var connection = _dapperContext.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("userId", id, DbType.Int32);

        var user = await connection.QuerySingleOrDefaultAsync<User>(query, parameters);
        
        return user;
    }

    public async Task ChangePassword(int userId, string newPassword)
    {
        var query = @"UPDATE Users SET [Password] = @password WHERE Id = @userId";

        var parameters = new DynamicParameters();
        parameters.Add("userId", userId, DbType.Int32);
        parameters.Add("password", newPassword, DbType.String);

        using var connection = _dapperContext.CreateConnection();

        await connection.ExecuteAsync(query, parameters);
    }

    public async Task DeleteUser(int userId)
    {
        using var connection = _dapperContext.CreateConnection();

        await DeleteFromUserRolesTable(userId, connection);

        await DeleteFromUsersTable(userId, connection);
    }

    public async Task AdminUpdateUser(int id, UserDto user)
    {
        var query = @"UPDATE Users SET Username = @username, [Password] = @password WHERE Id = @id";

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);

        var parameters = new DynamicParameters();
        parameters.Add("id", id, DbType.Int32);
        parameters.Add("username", user.Username, DbType.String);
        parameters.Add("password", hashedPassword, DbType.String);

        using var connection = _dapperContext.CreateConnection();

        await connection.ExecuteAsync(query, parameters);
    }
    
    public async Task<List<string>> GetUserRoles(int userId)
    {
        var query = @"SELECT Role FROM UserRoles INNER JOIN Roles ON UserRoles.RoleId = Roles.Id WHERE UserId = @userId";
        var parameters = new DynamicParameters();
        parameters.Add("userId", userId, DbType.Int32);

        using var connection = _dapperContext.CreateConnection();

        var roles = await connection.QueryAsync(query, parameters);
        var rolesList = new List<string>();
        foreach (var roleObj in roles)
        {
            rolesList.Add(roleObj.Role);
        }
        
        return rolesList;
    }
    
    public async Task InsertIntoUserRolesTable(int userId)
    {
        var query = $"INSERT INTO UserRoles (UserId, RoleId) VALUES (@userId, {DefaultRoleId})";
        
        var parameters = new DynamicParameters();
        parameters.Add("userId", userId, DbType.Int32);

        using var connection = _dapperContext.CreateConnection();
        
        await connection.ExecuteAsync(query, parameters);
    }
    
    private async Task DeleteFromUserRolesTable(int userId, IDbConnection connection)
    {
        var query = @"DELETE FROM UserRoles WHERE UserId = @userId";

        var parameters = new DynamicParameters();
        parameters.Add("userId", userId, DbType.Int32);

        await connection.ExecuteAsync(query, parameters);
    }
    
    private async Task DeleteFromUsersTable(int userId, IDbConnection connection)
    {
        var query = @"DELETE FROM Users WHERE Id = @userId";
        
        var parameters = new DynamicParameters();
        parameters.Add("userId", userId, DbType.Int32);

        await connection.ExecuteAsync(query, parameters);
    }
}