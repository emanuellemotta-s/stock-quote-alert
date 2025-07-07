using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using System.IO;

class Program
{
    // Função para enviar email
    static void SendEmail(string subject, string body, decimal preco)
    {
        var config = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .Build();

        var alertSettings = config.GetSection("AlertSettings").Get<AlertSettings>();

        if (alertSettings == null || alertSettings.Smtp == null)
        {
            Console.WriteLine("Configuração de alerta ou SMTP ausente.");
            return;
        }

        var smtpClient = new SmtpClient(alertSettings.Smtp.Host, alertSettings.Smtp.Port)
        {
            Credentials = new NetworkCredential(alertSettings.Smtp.Username, alertSettings.Smtp.Password),
            EnableSsl = alertSettings.Smtp.EnableSsl
        };

        var mail = new MailMessage(
                    from: alertSettings.Smtp.Username,
                    to: alertSettings.DestinationEmail,
                    subject: $"{subject}",
                    body: $"{body}"
                );

        smtpClient.Send(mail);


    }

    // Função que utiliza API que retorna preço atual da cotação de um ativo da B3
    static async Task<decimal> PrecoAtivo(string token, string url)
    {
        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string dados = await response.Content.ReadAsStringAsync();

            using JsonDocument doc = JsonDocument.Parse(dados);
            var ativo = doc.RootElement.GetProperty("results")[0];

            decimal preco = ativo.GetProperty("regularMarketPrice").GetDecimal();

            return preco;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
            return -1m;
        }

    }

    // Função Main 
    static async Task Main(string[] args)
    {

        if (args.Length >= 3)
        {

            string token = "jNZViobwKBdNdssTY2w5mP";
            string ticker = args[0];
            string url = $"https://brapi.dev/api/quote/{ticker}?token={token}";

            decimal preco = await PrecoAtivo(token, url);

            if (decimal.TryParse(args[1], out decimal preco_venda) &&
            decimal.TryParse(args[2], out decimal preco_compra))
            {
                if (preco > preco_venda)
                {
                    string subject = "Alerta de Venda";
                    string body = $"Preço Atual: R$ {preco}.\nÉ aconselhável vender.";
                    SendEmail(subject, body, preco);
                    Console.WriteLine("Email enviado (venda)");
                }
                else if (preco < preco_compra)
                {
                    string subject = "Alerta de Compra";
                    string body = $"Preço Atual: R$ {preco}.\nÉ aconselhável comprar.";
                    SendEmail(subject, body, preco);
                    Console.WriteLine("Email enviado (compra)");
                }
                else
                {
                    string subject = "Alerta";
                    string body = $"Preço Atual: R$ {preco}.";
                    SendEmail(subject, body, preco);
                    Console.WriteLine("Email enviado");
                }
            }
            else
            {
                Console.WriteLine("Preços inválidos. Use números válidos como argumentos.");
                return;
            }

        }
        else
        {
            Console.WriteLine("Está faltando um ou mais dos seguintes parâmetros: Ativo, Preço de Referência para Venda e Preço de Referência para Compra.");
        }

    }

}