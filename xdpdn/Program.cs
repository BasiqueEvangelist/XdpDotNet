using System.CommandLine;
using Microsoft.Win32.SafeHandles;
using Tmds.DBus.Protocol;
using XdgDesktopPortal;
using XdgDesktopPortal.Services;

Connection conn = new Connection(Address.Session!);
await conn.ConnectAsync();

var root = new RootCommand("Utility for testing Xdp.Net");

{
    Command composeEmail = new Command("compose-email", "Composes an email message.");
    var toOption = new Option<List<string>>("--to")
    {
        Arity = ArgumentArity.ZeroOrMore,
        IsRequired = false
    };

    var ccOption = new Option<List<string>>("--cc")
    {
        Arity = ArgumentArity.ZeroOrMore,
        IsRequired = false
    };

    var bccOption = new Option<List<string>>("--bcc")
    {
        Arity = ArgumentArity.ZeroOrMore,
        IsRequired = false
    };

    var subjectOption = new Option<string>("--subject")
    {
        IsRequired = false
    };

    var bodyOption = new Option<string>("--body")
    {
        IsRequired = false
    };

    composeEmail.AddOption(toOption);
    composeEmail.AddOption(ccOption);
    composeEmail.AddOption(bccOption);
    composeEmail.AddOption(subjectOption);
    composeEmail.AddOption(bodyOption);

    composeEmail.SetHandler(async (to, cc, bcc, subject, body) =>
    {
        var email = new XdpEmail(conn);

        await email.ComposeEmail(default, new EmailMessage
        {
            Addresses = to,
            Cc = cc,
            Bcc = bcc,
            Subject = subject,
            Body = body
        });
    }, toOption, ccOption, bccOption, subjectOption, bodyOption);

    root.AddCommand(composeEmail);
}

{
    Command retrieveSecret = new Command("retrieve-secret", "Retrieves the application-specific secret");

    retrieveSecret.SetHandler(async () =>
    {
        var secret = new XdpSecret(conn);

        Console.WriteLine(Convert.ToHexString(await secret.RetrieveSecret()));
    });

    root.AddCommand(retrieveSecret);
}

{
    Command trashFile = new Command("trash-file", "Moves a file to the trash");

    var fileArg = new Argument<List<FileInfo>>("files", "Files to send to trash") {
        Arity = ArgumentArity.OneOrMore,
    };

    trashFile.AddArgument(fileArg);

    trashFile.SetHandler(async (files) =>
    {
        var trash = new XdpTrash(conn);

        foreach (var file in files) {
            using var stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.Delete);

            await trash.TrashFile(stream.SafeFileHandle);
        }
    }, fileArg);

    root.AddCommand(trashFile);
}

{
    Command openUri = new Command("open-uri", "Opens a URI in the appropriate application");

    var uriArg = new Argument<Uri>("uri", "the URI to open");
    var askOpt = new Option<bool>(["--ask", "-a"], "Always ask the user to pick the application");

    openUri.AddArgument(uriArg);
    openUri.AddOption(askOpt);

    openUri.SetHandler(async (uri, ask) =>
    {
        var openUri = new XdpOpenURI(conn);

        await openUri.OpenURI(default, uri, ask: ask);
    }, uriArg, askOpt);

    root.AddCommand(openUri);
}

{
    Command openFile = new Command("open-file", "Opens a file in the appropriate application");

    var fileArg = new Argument<FileInfo>("file", "the file to open");
    var askOpt = new Option<bool>(["--ask", "-a"], "Always ask the user to pick the application");

    openFile.AddArgument(fileArg);
    openFile.AddOption(askOpt);

    openFile.SetHandler(async (file, ask) =>
    {
        var openUri = new XdpOpenURI(conn);
        await openUri.OpenFile(default, file, ask: ask);
    }, fileArg, askOpt);

    root.AddCommand(openFile);
}

{
    Command openDir = new Command("open-dir", "Opens a directory in the appropriate application");

    var dirArg = new Argument<DirectoryInfo>("dir", "the directory to open");
    var askOpt = new Option<bool>(["--ask", "-a"], "Always ask the user to pick the application");

    openDir.AddArgument(dirArg);
    openDir.AddOption(askOpt);

    openDir.SetHandler(async (dir, ask) =>
    {
        var openUri = new XdpOpenURI(conn);
        await openUri.OpenDirectory(default, dir);
    }, dirArg, askOpt);

    root.AddCommand(openDir);
}

{
    Command getUserInfo = new Command("get-user-info", "Requests information about the user");

    var reasonOpt = new Option<string?>(["-r", "--reason"], "Specifies the reason provided to the user") {
        Arity = ArgumentArity.ZeroOrOne,
        IsRequired = false
    };

    getUserInfo.AddOption(reasonOpt);

    getUserInfo.SetHandler(async (reason) =>
    {
        var account = new XdgAccount(conn);
        var info = await account.GetUserInformation(default, reason);

        Console.WriteLine($"ID: {info.Id}");
        Console.WriteLine($"Name: {info.Name}");
        Console.WriteLine($"Image: {info.Image}");
    }, reasonOpt);

    root.AddCommand(getUserInfo);
}

return await root.InvokeAsync(args);