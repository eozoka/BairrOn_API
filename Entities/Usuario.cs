namespace BairrOn.Api.Entities;
public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string EmailNormalizado { get; set; } = "";
    // guarda a senha protegida pelo hash
    public string SenhaHash { get; set; } = "";
    public int VersaoToken { get; set; }
}
