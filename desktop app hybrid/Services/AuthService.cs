//using Accessibility;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;

namespace desktop_app_hybrid.Services
{
    public class AuthService
    {
        public SmtpClient _smtp;

        private readonly string _cs;

        public AuthService(IConfiguration config)
        {
            _smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential
                (
                    "studentgrades37@gmail.com",
                    "zxsg scnp aiyo xlco"
                )
            };

            _cs = config.GetConnectionString("Trueone")
                ?? throw new InvalidOperationException("Connection string 'Default' not found");
        }

        public async Task<bool> LoginAsync(string username, string password, string role)
        {
            try
            {
                await using var conn = new SqlConnection(_cs);
                await conn.OpenAsync();
                int ist = role == "etudiants" ? 1 : 0;
                int it = role == "enseignants" ? 1 : 0;
                const string sql = """ select dbo.CheckLogin(@u,@p,@ist,@it)""";

                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@u", username);
                cmd.Parameters.AddWithValue("@p", password);
                cmd.Parameters.AddWithValue("@ist", ist);
                cmd.Parameters.AddWithValue("@it", it);

                return Convert.ToBoolean(await cmd.ExecuteScalarAsync());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw; // important pour voir l’erreur exacte
            }
        }

        public async Task<int> SignupAsync(string username, string mail, string password, int it, int ist, string noms, string prenoms, string role)
        {
            try
            {
                await using var conn = new SqlConnection(_cs);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand("dbo.sp_createuser", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@mail", mail);
                cmd.Parameters.AddWithValue("@password", password);
                cmd.Parameters.AddWithValue("@it", it);
                cmd.Parameters.AddWithValue("@ist", ist);
                cmd.Parameters.AddWithValue("@nom", noms);
                cmd.Parameters.AddWithValue("@prenom", prenoms);
                cmd.Parameters.AddWithValue("@role", role);

                var returnparam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
                returnparam.Direction = ParameterDirection.ReturnValue;
                bool res=false;
                await cmd.ExecuteNonQueryAsync();
                int result = (int)returnparam.Value;
                Debug.WriteLine($"{username} , {mail} , {password} , {it} , {ist} , {noms} , {prenoms} , {role}");
                Debug.WriteLine($"success {result} {role}");
                if (result == 1) { 
                long timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string code = timestampMs.ToString().Substring(7, 6);
                 res=await sendotp(mail, code, username);
                }
                return result;
            }
            catch (SqlException ex)
            {
                Debug.WriteLine("SQL ERROR: " + ex.Message);
                throw; // garde-le pour voir l'erreur exacte
            }
        }

        public async Task<DataTable> Selector(string columns, string table, string condition = "")
        {
            DataTable dataTable = new DataTable();

            await using SqlConnection conn = new SqlConnection(_cs);
            await conn.OpenAsync();

            string sql = $"SELECT {columns} FROM {table}";
            if (!string.IsNullOrWhiteSpace(condition))
            {
                sql += $" WHERE {condition}";
            }

            await using SqlCommand cmd = new SqlCommand(sql, conn);
            await using SqlDataReader reader = await cmd.ExecuteReaderAsync();

            dataTable.Load(reader);
            Debug.WriteLine($"Selector executed: {sql}, Rows returned: {dataTable.Rows.Count}");
            return dataTable;
        }

public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var fromEmail = "studentgrades37@gmail.com";
            var password = "zxsg scnp aiyo xlco"; // mot de passe d'application Gmail

            var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(fromEmail, password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, "Student-Grade Academy"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
                if (!toEmail.Contains("gmail"))
                {
                    return true;
                }
            mailMessage.To.Add(toEmail);

            await smtp.SendMailAsync(mailMessage);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erreur envoi mail: " + ex.Message);
            return false;
        }
    }
public async Task<bool> sendotp(string toemail , string otp,string username)
        {
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.sp_setotp", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@code", Convert.ToInt32(otp));
            var returnparam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnparam.Direction = ParameterDirection.ReturnValue;
            await cmd.ExecuteNonQueryAsync();
            int result = (int)returnparam.Value;
            Debug.WriteLine($"success {result} ");
            string template = $$"""
                                <!DOCTYPE html>
                <html lang="fr">
                <head>
                    <meta charset="UTF-8">
                    <title>Vérification de votre adresse email</title>
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <style>
                        body {
                            margin: 0;
                            padding: 0;
                            background: #f3f4f6;
                            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
                            color: #111827;
                        }
                        .wrapper {
                            width: 100%;
                            padding: 24px 0;
                        }
                        .container {
                            max-width: 520px;
                            margin: 0 auto;
                            background: #ffffff;
                            border-radius: 12px;
                            box-shadow: 0 8px 24px rgba(15, 23, 42, 0.15);
                            overflow: hidden;
                        }
                        .header {
                            padding: 16px 24px;
                            border-bottom: 1px solid #e5e7eb;
                            background: linear-gradient(135deg, #1d4ed8 0%, #2563eb 40%, #0ea5e9 100%);
                            color: #ffffff;
                        }
                        .header h1 {
                            margin: 0;
                            font-size: 20px;
                        }
                        .content {
                            padding: 24px;
                        }
                        .content h2 {
                            font-size: 18px;
                            margin: 0 0 8px;
                            color: #111827;
                        }
                        .content p {
                            font-size: 14px;
                            line-height: 1.6;
                            margin: 0 0 12px;
                            color: #374151;
                        }
                        .otp-box {
                            text-align: center;
                            margin: 22px 0;
                        }
                        .otp-code {
                            display: inline-block;
                            background: #111827;
                            color: #ffffff;
                            padding: 12px 24px;
                            border-radius: 999px;
                            font-size: 22px;
                            letter-spacing: 0.35em;
                            font-weight: 700;
                        }
                        .meta {
                            font-size: 12px;
                            color: #6b7280;
                            margin-top: 8px;
                        }
                        .footer {
                            padding: 16px 24px 20px;
                            border-top: 1px solid #e5e7eb;
                            font-size: 11px;
                            color: #9ca3af;
                            text-align: center;
                        }
                        a {
                            color: #2563eb;
                            text-decoration: none;
                        }
                        @media (max-width: 600px) {
                            .container {
                                border-radius: 0;
                            }
                            .content {
                                padding: 20px 16px;
                            }
                            .header {
                                padding: 14px 16px;
                            }
                        }
                    </style>
                </head>
                <body>
                <div class="wrapper">
                    <div class="container">
                        <div class="header">
                            <h1>Vérification de votre compte GradeMaster</h1>
                        </div>
                        <div class="content">
                            <h2>Bonjour {{username}},</h2>
                            <p>
                                Merci de vous être inscrit sur <strong>GradeMaster</strong>.  
                                Pour finaliser la création de votre compte et vérifier votre adresse email,
                                veuillez utiliser le code de vérification ci-dessous :
                            </p>

                            <div class="otp-box">
                                <span class="otp-code">{{otp}}</span>
                                <div class="meta">
                                    Ce code est valable pendant 10 minutes.  
                                    Ne le partagez jamais avec personne.
                                </div>
                            </div>

                            <p>
                                Si vous n’êtes pas à l’origine de cette demande, vous pouvez ignorer cet email.
                                Votre compte restera inchangé.
                            </p>

                            <p>
                                Cordialement,<br/>
                                L’équipe GradeMaster
                            </p>
                        </div>
                        <div class="footer">
                            Cet email a été envoyé automatiquement, merci de ne pas y répondre.<br/>
                            © 2025 GradeMaster. Tous droits réservés.
                        </div>
                    </div>
                </div>
                </body>
                </html>
                
                """;
            if(result==1)
            {
                string subject = "Votre code OTP";
                string body =template;
             await SendEmailAsync(toemail, subject, body);
            }
            return result == 1;

        }
        public async Task<bool> Verifyotp( int otp, string username)
        {
            try
            {
                await using var conn = new SqlConnection(_cs);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand("dbo.sp_verifyotp", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@code", otp);
                var returnparam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
                returnparam.Direction = ParameterDirection.ReturnValue;
                await cmd.ExecuteNonQueryAsync();
                int result = (int)returnparam.Value;
                Debug.WriteLine($"success {result} ");
                return result == 1;
            }
            catch (SqlException ex)
            {
                Debug.WriteLine("SQL ERROR: " + ex.Message);
                throw; // garde-le pour voir l'erreur exacte
            }
        }
        public async Task<bool> updater(string nom, string prenom, string num, string classe, string filliere, string addresse, int abs, char sexe, int ret, string mailoru, string role, DateTime dob, string link,int status,DateTime doa)
        {
            try
            {
                await using var conn = new SqlConnection(_cs);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand("dbo.sp_updater", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@nom", nom);
                cmd.Parameters.AddWithValue("@prenom", prenom);
                cmd.Parameters.AddWithValue("@num", num);
                cmd.Parameters.AddWithValue("@classe", classe);
                cmd.Parameters.AddWithValue("@filliere", filliere);
                cmd.Parameters.AddWithValue("@addresse", addresse);
                cmd.Parameters.AddWithValue("@abs", abs);
                cmd.Parameters.AddWithValue("@sexe", sexe);
                //cmd.Parameters.AddWithValue("@ret", ret);
                cmd.Parameters.AddWithValue("@emailorusername", mailoru);
                cmd.Parameters.AddWithValue("@role", role);
                cmd.Parameters.AddWithValue("@dob", dob);
                cmd.Parameters.AddWithValue("@link", link);
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@doa", doa);

                var returnparam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
                returnparam.Direction = ParameterDirection.ReturnValue;

                await cmd.ExecuteNonQueryAsync();
                int result = (int)returnparam.Value;
                Debug.WriteLine($"success {result} ");
                return result == 1;
            }
            catch (SqlException ex)
            {
                Debug.WriteLine("SQL ERROR: " + ex.Message);
                throw; // garde-le pour voir l'erreur exacte
            }
        }

        public async Task<bool> subjectadd(string nom, string mailorusername, string classe, string filiere, int credits, int semestre)
        {


            try
            {
                await using var conn = new SqlConnection(_cs);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand("dbo.sp_addsubject", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@nom", nom);
                cmd.Parameters.AddWithValue("@mailorusername", mailorusername);
                cmd.Parameters.AddWithValue("@filiere", filiere);
                cmd.Parameters.AddWithValue("@credits", credits);
                cmd.Parameters.AddWithValue("@semestre", semestre);
                cmd .Parameters.AddWithValue("@classe", classe);
                var returnparam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
                returnparam.Direction = ParameterDirection.ReturnValue;
                await cmd.ExecuteNonQueryAsync();
                int result = (int)returnparam.Value;
                Debug.WriteLine($"success {result} ");
                return result == 1;
            }
            catch (SqlException ex)
            {
                Debug.WriteLine("SQL ERROR: " + ex.Message);
                throw; // garde-le pour voir l'erreur exacte
            }




        }

        public async Task<bool> subjectdelete(string nom, string mailorusername)
        {
            try
            {
                await using var conn = new SqlConnection(_cs);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand("dbo.sp_deletesubject", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@nom", nom);
                cmd.Parameters.AddWithValue("@mailorusername", mailorusername);
                var returnparam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
                returnparam.Direction = ParameterDirection.ReturnValue;
                await cmd.ExecuteNonQueryAsync();
                int result = (int)returnparam.Value;
                Debug.WriteLine($"success {result} ");
                return result == 1;

            }
            catch (SqlException ex)
            {
                Debug.WriteLine("SQL ERROR: " + ex.Message);
                throw; // garde-le pour voir l'erreur exacte
            }
        }
        public async Task<bool> delstudent(int id)
        {
            try { 
            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.sp_deletestudent", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@id", id);
            var returnparam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnparam.Direction = ParameterDirection.ReturnValue;
            await cmd.ExecuteNonQueryAsync();
            int result = (int)returnparam.Value;
            Debug.WriteLine($"success {result} ");
            return result == 1;

        }
            catch (SqlException ex)
            {
                Debug.WriteLine("SQL ERROR: " + ex.Message);
                throw; // garde-le pour voir l'erreur exacte
            }
}

        public async Task<bool> EditSubject(string oldNom, string newNom, string mailorusername,string classe, string filiere, int credits, int semestre)
        {
            try
            {
                await using var conn = new SqlConnection(_cs);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand("dbo.sp_editsubject", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@oldNom", oldNom);
                cmd.Parameters.AddWithValue("@newNom", newNom);
                cmd.Parameters.AddWithValue("@mailorusername", mailorusername);
                cmd.Parameters.AddWithValue("@filiere", filiere);
                cmd.Parameters.AddWithValue("@credits", credits);
                cmd.Parameters.AddWithValue("@semestre", semestre);
                cmd.Parameters.AddWithValue("@classe", classe);
                var returnparam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
                returnparam.Direction = ParameterDirection.ReturnValue;
                await cmd.ExecuteNonQueryAsync();
                int result = (int)returnparam.Value;
                Debug.WriteLine($"success {result} ");
                return result == 1;
            }
            catch (SqlException ex)
            {
                Debug.WriteLine("SQL ERROR: " + ex.Message);
                throw; // garde-le pour voir l'erreur exacte
            }
        } 

        public async Task<bool> Editgrades(int subjectid,int studentid, double val, string type)
        {

            try
            {
                await using var conn = new SqlConnection(_cs);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand("dbo.sp_editgrades", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@subid", subjectid);
                cmd.Parameters.AddWithValue("@studid", studentid);
                cmd.Parameters.AddWithValue("@val", (float)val);
                cmd.Parameters.AddWithValue("@type", type);
                var returnparam = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
                returnparam.Direction = ParameterDirection.ReturnValue;
                await cmd.ExecuteNonQueryAsync();
                int result = (int)returnparam.Value;
                Debug.WriteLine($"success {result} ");
                return result == 1;
            }
            catch (SqlException ex)
            {
                Debug.WriteLine("SQL ERROR: " + ex.Message);
                throw; // garde-le pour voir l'erreur exacte
            }
        }


    }
}