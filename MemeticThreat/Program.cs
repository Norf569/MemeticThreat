using Microsoft.Extensions.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MemeticThreatServerAPI;
using MemeticThreatServerAPI.Models;
using MemeticThreatServerAPI.Data;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using System.Text.Json;

var builder = WebApplication.CreateBuilder();

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey()
    };
});

string connection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ModelsDbContext>(options => options.UseSqlServer(connection));

var contextOptions = new DbContextOptionsBuilder<ModelsDbContext>().UseSqlServer(connection).Options;

builder.Services.AddSingleton<ModelsDbContext>(new ModelsDbContext(contextOptions));

var app = builder.Build();

const string fileFolderPath = "E:\\PSU\\5\\sp\\MemThreat\\MemeticThreat\\MemeticThreat\\files\\";
const long BASE_DIVIDER = 1000000000;

app.MapGet("/login", (ModelsDbContext db, HttpContext context, string name, string password) =>
{
    User? user = db.Users.FirstOrDefault(u => u.Name == name && u.Password == password);
    if (user == null)
    {
        context.Response.StatusCode = 401;
        return;
    }

    //
    string jsonUser = JsonSerializer.Serialize(user);
    jsonUser = jsonUser.Substring(0, jsonUser.Length - 1) + ",\"Jwt\":\"" + GetJwtAsString(user) + "\"}";

    context.Response.StatusCode = 200;
    context.Response.WriteAsync(jsonUser);
});

app.MapGet("/register", (ModelsDbContext db, HttpContext context, string name, string email, string password) =>
{
    User? user = db.Users.FirstOrDefault(u => u.Name == name || u.Email == email);
    if (user != null)
    {
        context.Response.StatusCode = 400;
        return;
    }

    user = new User { Name = name, Email = email, Password = password };
    db.Add(user);
    db.SaveChanges();

    //
    string jsonUser = JsonSerializer.Serialize(user);
    jsonUser = jsonUser.Substring(0, jsonUser.Length - 1) + ",\"Jwt\":\"" + GetJwtAsString(user) + "\"}";

    context.Response.StatusCode = 200;
    context.Response.WriteAsync(jsonUser);
});

app.MapDelete("/deleteUser", [Authorize] (ModelsDbContext db, HttpContext context) =>
{
    string userName = context.User.FindFirstValue(ClaimTypes.Name);
    User user = db.Users.FirstOrDefault(u => u.Name == userName);

    try
    {
        foreach (var fileModel in user.FileModels)
        {
            new FileInfo(fileFolderPath + fileModel.Path + fileModel.FileName).Delete();
            db.Remove(fileModel);
            db.SaveChanges();
            break;
        }
        if(Directory.Exists(fileFolderPath + userName))
            Directory.Delete(fileFolderPath + userName, true);
    } catch (Exception ex)
    {
        Trace.WriteLine(ex.Message);
        return "Error";
    }

    db.Remove(user);
    db.SaveChanges();
    return "User removed";
});

//
app.MapGet("", [Authorize] async (HttpContext context, string filePath) =>
{
    filePath = fileFolderPath + filePath;
    if (!File.Exists(filePath))
    {   
        context.Response.StatusCode = 404;
        return;
    }
    string fileName = filePath.Split('\\').Last();
    try
    {
        using (System.IO.Stream oStream = new System.IO.FileStream(filePath,
                mode: System.IO.FileMode.Open,
                share: System.IO.FileShare.Read,
                access: System.IO.FileAccess.Read))
        {
            long fileLenght = oStream.Length;

            context.Response.ContentType = "application/octet-stream";
            context.Response.ContentLength = fileLenght;
            context.Response.Headers.Append("FileName", fileName);
            context.Response.Headers.Append("FileLength", fileLenght.ToString());

            await oStream.CopyToAsync(context.Response.Body);
        }
    } catch 
    {
        context.Response.StatusCode = 500;
    }
});

app.MapGet("/view", [Authorize] async (ModelsDbContext db, HttpContext context) =>
{
    string userName = context.User.FindFirstValue(ClaimTypes.Name);
    User? user = db.Users.FirstOrDefault(u => u.Name == userName);

    if (user == null)
    {
        context.Response.StatusCode = 401;
    }


    //string rootUserDir = fileFolderPath + userName + "\\";
    List<FileModel> files = db.FileModels.Where(o => o.UserId == user.Id).ToList();

    //foreach (var fileModel in user.FileModels)
    //{
    //    if (fileModel.Path.StartsWith(userName))
    //    {
    //        //string str = fileModel.Id.ToString() + " " + fileModel.Path.Remove(0, rootUserDir.Length) + fileModel.FileName + '\n';
    //        files.Add(fileModel);
    //        //await context.Response.WriteAsJsonAsync<FileModel>(fileModel);
    //    }
    //    //await context.Response.WriteAsJsonAsync(fileModel);
    //}
    await context.Response.WriteAsJsonAsync<List<FileModel>>(files);
});

app.MapGet("/storage", [Authorize] async (ModelsDbContext db, HttpContext context) =>
{
    string userName = context.User.FindFirstValue(ClaimTypes.Name);
    User? user = db.Users.FirstOrDefault(u => u.Name == userName);

    if (user == null)
    {
        context.Response.StatusCode = 401;
    }

    context.Response.StatusCode = 200;
    await context.Response.WriteAsync(user.TotalFileSize.ToString());
});

app.MapPost("", [Authorize] async (ModelsDbContext db, HttpContext context) =>
{
    try
    {
        string userName = context.User.FindFirstValue(ClaimTypes.Name);
        User? user = db.Users.FirstOrDefault(u => u.Name == userName); 

        if (user == null)
        {
            context.Response.StatusCode = 401;
            return;
        }

        StringValues sv;
        context.Request.Headers.TryGetValue("FileName", out sv);
        string fileName = sv.ToString();

        context.Request.Headers.TryGetValue("BaseFileName", out sv);
        string baseFileName = sv.ToString();

        context.Request.Headers.TryGetValue("FileSize", out sv);
        long filesSize = Convert.ToInt64(sv.ToString());
        if (filesSize > user.StorageSpace - user.TotalFileSize)
        {
            context.Response.StatusCode = 400;
            return;
        }

        string uploadPath = fileFolderPath + userName + "\\";
        string path = uploadPath + baseFileName + ".folder\\";

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        else if (File.Exists(path + fileName))
        {
            int fileCount = FileMerge(db, user.Id, uploadPath, path, fileName, baseFileName);
            context.Response.Headers.Append("UploadedFilesCount", fileCount.ToString());
            context.Response.StatusCode = 200;
            return;
        }

        System.IO.Stream oStream = new System.IO.FileStream(path: path + fileName,
                                                            mode: System.IO.FileMode.Create,
                                                            share: System.IO.FileShare.None,
                                                            access: System.IO.FileAccess.Write);
        await context.Request.Body.CopyToAsync(oStream);
        oStream.Dispose();

        int filesCount = FileMerge(db, user.Id, uploadPath, path, fileName, baseFileName);
        context.Response.Headers.Append("UploadedFilesCount", filesCount.ToString());
        context.Response.StatusCode = 200;
    }
    catch (Exception ex) 
    {
        Trace.WriteLine(ex.ToString()); 
        context.Response.StatusCode = 500;
    }
});

app.MapDelete("", [Authorize] async (ModelsDbContext db, HttpContext context, int id) =>
{
    string userName = context.User.FindFirstValue(ClaimTypes.Name);
    User? user = db.Users.FirstOrDefault(u => u.Name == userName);

    try
    {
        foreach (var fileModel in user.FileModels)
        {
            if (fileModel.Id == id)
            {
                new FileInfo(fileFolderPath + fileModel.Path + fileModel.FileName).Delete();
                db.Remove(fileModel);
                user.TotalFileSize -= fileModel.FileSize;
                db.SaveChanges();
                break;
            }
        }
    }
    catch (Exception ex) { Trace.WriteLine(ex.Message); }
});

app.Run();

int FileMerge(ModelsDbContext db, int userId, string uploadPath, string path, 
    string fileName, string baseFileName)
{
    int count = Convert.ToInt32(fileName.Split('.').Last());

    List<string> fileList = Directory.GetFiles(path).ToList();

    if (fileList.Count == count && !File.Exists(uploadPath + baseFileName))
    {
        System.IO.Stream oStream = new System.IO.FileStream(path: uploadPath + baseFileName,
                                                            mode: System.IO.FileMode.Create,
                                                            share: System.IO.FileShare.None,
                                                            access: System.IO.FileAccess.Write);

        SortedList<int, string> sortedFileList = new SortedList<int, string>();
        foreach (string file in fileList)
        {
            sortedFileList.Add(Convert.ToInt32(file.Split('_').Last().Split('.').First()), file);
        }

        long fileSize = 0;

        foreach (string fileString in sortedFileList.Values)
        {
            var stream = new System.IO.FileStream(fileString, FileMode.Open, FileAccess.Read);
            fileSize += stream.Length;
            stream.CopyTo(oStream);
            stream.Dispose();
        }

        oStream.Dispose();

        AddToDB(db, userId, fileSize, uploadPath, baseFileName);

        while (Directory.Exists(path))
        {
            try
            {
                Directory.Delete(path, true);
            }
            catch { };
        }
        return count;
    }
    return fileList.Count;
}

void AddToDB(ModelsDbContext db, int userId, long fileSize, string path, string fileName)
{
    FileModel newFileModel = new FileModel { FileName = fileName, 
        Path = path.Remove(0, fileFolderPath.Length), 
        FileSize = fileSize, 
        UserId = userId };
 
    if (!IsExist(db, path, fileName))
    {
        db.Add(newFileModel);
        db.Users.First(u => u.Id == userId).TotalFileSize += fileSize;
        db.SaveChanges();
    }
    else
        Console.WriteLine("Paths already exists!");
}

bool IsExist(ModelsDbContext db, string path, string fileName)
{
    foreach (var fileModel in db.FileModels)
        if (fileModel.FileName == fileName && fileModel.Path == path)
            return true;
    return false;
}

string GetJwtAsString(User user)
{
    var claims = new List<Claim> { new Claim(ClaimTypes.Name, user.Name) };
    var jwt = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.UtcNow.Add(TimeSpan.FromHours(1)),
        signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

    return new JwtSecurityTokenHandler().WriteToken(jwt);
}