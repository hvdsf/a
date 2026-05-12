using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Pm.Data;
using Pm.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace Pm.Controllers;

[Route("api/[controller]")]
[ApiController]
[SwaggerTag("Operações de Autenticação e Gestão de Contas")]
public class AuthController(AppDbContext context, IConfiguration configuration) : ControllerBase
{
    [HttpPost("register")]
    [SwaggerOperation(
        Summary = "Registra um novo usuário.",
        Description = "A senha será criptografada. O papel padrão é 'Basic'."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Usuário registrado com sucesso!")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Se o e-mail já estiver em uso no banco de dados.", typeof(string))]
    public async Task<IActionResult> Register([FromBody] RegisterDto request)
    {
        if (await context.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest("E-mail já está em uso.");

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            // Criptografa a senha antes de salvar
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = Roles.Basic // Role padrão
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return Ok("Usuário registrado com sucesso!");
    }

    [HttpPost("login")]
        [SwaggerOperation(
        Summary = "Realiza login."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "{ token: \"token\" }")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Credenciais inválidas.", typeof(string))]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        // Verifica se o usuário existe e se a senha está correta
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Credenciais inválidas.");

        var token = GenerateJwtToken(user);

        return Ok(new { Token = token });
    }

    private string GenerateJwtToken(User user)
    {
        var jwtKey = configuration["Jwt:Key"];
        var key = Encoding.ASCII.GetBytes(jwtKey!);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            // Adiciona informações (Claims) dentro do token
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()) // Importante para as permissões
            ]),
            Expires = DateTime.UtcNow.AddHours(2), // Token válido por 2 horas
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}

[SwaggerSchema("Dados necessários para o registro de um novo usuário.")]
public record RegisterDto(
    
    [Required]
    string Name,

    [Required, EmailAddress]
    string Email,

    [Required, MinLength(6)]
    string Password
);

[SwaggerSchema("Dados necessários para o realizar login.")]
public record LoginDto(string Email, string Password);