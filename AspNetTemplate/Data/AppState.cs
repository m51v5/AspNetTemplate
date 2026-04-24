using AspNetTemplate.Core.Infra.Extensions;

namespace AspNetTemplate.Data
{
    public static class AppState
    {
        public static IConfiguration? Config { get; set; }

        public static string UploadsPath => $"{AppData}/{UploadsUrl}";
        public static string UploadsUrl => "uploads";
        public static string PrivatePath => $"{AppData}/private";
        public static string AppData => "data";

        public static string SuperAdminEmail => "admin@example.com";
        public static string SuperAdminPassword = "Password@123".AsHashedPassword();

        public static string C_str_PostgreSql => Config?.GetSection("ConnectionStrings:C_str_PostgreSql").Get<string>()
                    ?? throw new ArgumentNullException("ConnectionStrings:C_str_PostgreSql is required");

        public static string App_domain => Config?.GetSection("AppSetting:App_domain").Get<string>()
            ?? throw new ArgumentNullException("AppSetting:App_domain is required");

        public static string UploadsDomain => (Config?.GetSection("AppSetting:Uploads_Domain").Get<string>()
            ?? throw new ArgumentNullException("AppSetting:Uploads_Domain is required")) + $"/{UploadsUrl}/";

        public static string JwtSecretKey => Config?["Jwt:SecretKey"]
            ?? throw new ArgumentNullException("Jwt:SecretKey is required");

        #region Swagger

        public static string? Swagger_UserName => Config?.GetSection("SwaggerSetup:UserName").Get<string>();
        public static string? Swagger_Password => Config?.GetSection("SwaggerSetup:Password").Get<string>();
        public static string? Swagger_Doc_AppTitle => Config?.GetSection("SwaggerSetup:Doc:AppTitle").Get<string>();
        public static string? Swagger_Doc_AppDescription => Config?.GetSection("SwaggerSetup:Doc:Description").Get<string>();
        public static string? Swagger_Document_Name => Config?.GetSection("SwaggerSetup:Doc:Name").Get<string>();
        public static string? Swagger_Doc_Version => Config?.GetSection("SwaggerSetup:Doc:Version").Get<string>();

        #endregion
    }
}
