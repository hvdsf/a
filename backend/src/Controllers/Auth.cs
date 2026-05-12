using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Pm.Data;
using Pm.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Pm.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(AppDbContext context, IConfiguration configuration) : ControllerBase
{
    [HttpPost("register")]
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

// DTOs para receber os dados limpos
public record RegisterDto(string Name, string Email, string Password);
public record LoginDto(string Email, string Password);