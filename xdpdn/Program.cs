using System.CommandLine;
using System.CommandLine.Invocation;
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

    var fileArg = new Argument<List<FileInfo>>("files", "Files to send to trash")
    {
        Arity = ArgumentArity.OneOrMore,
    };

    trashFile.AddArgument(fileArg);

    trashFile.SetHandler(async (files) =>
    {
        var trash = new XdpTrash(conn);

        foreach (var file in files)
        {
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

    var reasonOpt = new Option<string?>(["-r", "--reason"], "Specifies the reason provided to the user")
    {
        Arity = ArgumentArity.ZeroOrOne,
        IsRequired = false
    };

    getUserInfo.AddOption(reasonOpt);

    getUserInfo.SetHandler(async (reason) =>
    {
        var account = new XdpAccount(conn);
        var info = await account.GetUserInformation(default, reason);

        Console.WriteLine($"ID: {info.Id}");
        Console.WriteLine($"Name: {info.Name}");
        Console.WriteLine($"Image: {info.Image}");
    }, reasonOpt);

    root.AddCommand(getUserInfo);
}

{
    Command screenshot = new Command("screenshot", "Takes a screenshot");

    var modalOpt = new Option<bool>(["-m", "--modal"], "Makes the user dialog modal");
    var interactiveOpt = new Option<bool>(["-i", "--interactive"], "Makes the user dialog interactive");

    screenshot.AddOption(modalOpt);
    screenshot.AddOption(interactiveOpt);

    screenshot.SetHandler(async (modal, interactive) =>
    {
        var screenshot = new XdpScreenshot(conn);
        var screenshotUri = await screenshot.Screenshot(default, modal, interactive);

        Console.WriteLine(screenshotUri);
    }, modalOpt, interactiveOpt);

    root.AddCommand(screenshot);
}

{
    Command pickColor = new Command("pick-color", "Picks a single pixel's color");

    pickColor.SetHandler(async () =>
    {
        var screenshot = new XdpScreenshot(conn);
        var color = await screenshot.PickColor(default);

        Console.WriteLine(color);
    });

    root.AddCommand(pickColor);
}

{
    Command openFileDialog = new Command("open-file-dialog", "Opens an open file dialog");

    var titleArg = new Argument<string>("title", "The title the dialog will have");
    var acceptLabelOpt = new Option<string?>(["--accept-label"], "Label for the accept button");
    var modalOpt = new Option<bool>(["--modal"], "Whether the dialog should be modal");
    var multipleOpt = new Option<bool>(["--multiple"], "Whether multiple files can be selected or not");
    var directoryOpt = new Option<bool>(["--directory"], "Whether to select for folders instead of files");

    var checkboxesOpt = new Option<List<string>>(["--checkbox"], "Adds a checkbox");

    openFileDialog.AddArgument(titleArg);
    openFileDialog.AddOption(acceptLabelOpt);
    openFileDialog.AddOption(modalOpt);
    openFileDialog.AddOption(multipleOpt);
    openFileDialog.AddOption(directoryOpt);
    openFileDialog.AddOption(checkboxesOpt);

    openFileDialog.SetHandler(async (title, acceptLabel, modal, multiple, directory, checkboxes) =>
    {
        var fileChooser = new XdpFileChooser(conn);

        var req = fileChooser.OpenFile(default, title);

        if (acceptLabel != null) req.AcceptLabel(acceptLabel);

        req.Modal(modal)
            .Multiple(multiple)
            .Directory(directory);

        foreach (var checkbox in checkboxes)
        {
            req.Choice(checkbox, checkbox);
        }

        // TODO: figure out why filters are broken.
        // req.Filter(new XdpFileChooser.FileFilter()
        // {
        //     Name = "Pictures",
        //     GlobFilters = ["*.jpg"],
        //     MimeTypeFilters = ["image/png"]
        // });

        var res = await req.Send();

        foreach (var uri in res.Uris) Console.WriteLine(uri);

        if (res.Choices != null)
            foreach (var choice in res.Choices) Console.WriteLine($"{choice.Key} -> {choice.Value}");

        if (res.CurrentFilter != null) Console.WriteLine($"filter: {res.CurrentFilter}");

    }, titleArg, acceptLabelOpt, modalOpt, multipleOpt, directoryOpt, checkboxesOpt);

    root.AddCommand(openFileDialog);
}

{
    Command addNotification = new Command("add-notification", "Sends a notification");

    var idArg = new Argument<string>("id", "Application-provided ID for this notification");
    addNotification.AddArgument(idArg);

    var titleOpt = new Option<string?>("--title", "User-visible string to display as the title");
    addNotification.AddOption(titleOpt);

    var bodyOpt = new Option<string?>("--body", "User-visible string to display as the body");
    addNotification.AddOption(bodyOpt);

    var iconOpt = new Option<FileInfo?>("--icon", "Image to use as the user-visible icon");
    addNotification.AddOption(iconOpt);

    var priorityOpt = new Option<XdpNotification.NotificationPriority?>("--priority", "The notification's priority");
    addNotification.AddOption(priorityOpt);

    addNotification.SetHandler(async (id, title, body, icon, priority) =>
    {
        var notificationSvc = new XdpNotification(conn);

        var notification = new XdpNotification.Notification()
        {
            Title = title,
            Body = body,

            Priority = priority
        };

        if (icon != null) notification.Icon = await File.ReadAllBytesAsync(icon.FullName);

        await notificationSvc.AddNotification(id, notification);
    }, idArg, titleOpt, bodyOpt, iconOpt, priorityOpt);

    root.AddCommand(addNotification);
}

{
    Command removeNotification = new Command("remove-notification", "Removes a notification");

    var idArg = new Argument<string>("id", "Application-provided ID for this notification");
    removeNotification.AddArgument(idArg);

    removeNotification.SetHandler(async (id) =>
    {
        var notificationSvc = new XdpNotification(conn);

        await notificationSvc.RemoveNotification(id);
    }, idArg);

    root.AddCommand(removeNotification);
}

{
    Command readAllSettings = new Command("read-all-settings", "Reads all settings");

    readAllSettings.SetHandler(new Func<Task>(async () =>
    {
        var settings = new XdpSettings(conn);

        var data = await settings.GetAll();

        foreach (var nsEntry in data)
        {
            Console.WriteLine(nsEntry.Key);

            foreach (var entry in nsEntry.Value)
            {
                Console.WriteLine("    {0}={1}", entry.Key, entry.Value);
            }
        }
    }));

    root.AddCommand(readAllSettings);
}

{
    Command transferFiles = new Command("transfer-files", "Transfers files via the FileTransfer portal");

    var filesArg = new Argument<FileInfo[]>("files")
    {
        Arity = ArgumentArity.OneOrMore
    };
    transferFiles.AddArgument(filesArg);

    var writableOpt = new Option<bool>("--writable");
    transferFiles.AddOption(writableOpt);
    
    var noAutoStopOpt = new Option<bool>("--no-autostop");
    transferFiles.AddOption(noAutoStopOpt);
    
    transferFiles.SetHandler(async (files, writable, noAutoStop) =>
    {
        var fileTransfer = new XdpFileTransfer(conn);

        await using var session = await fileTransfer.StartTransfer(writable: writable, autoStop: !noAutoStop);
        await session.AddFiles(files);
        
        Console.WriteLine(session.Key);

        await session.AwaitClosure();
    }, filesArg, writableOpt, noAutoStopOpt);

    root.AddCommand(transferFiles);
}

{
    Command receiveFiles = new Command("receive-files", "Receives files via the FileTransfer portal");

    var keyArg = new Argument<string>("key");
    receiveFiles.AddArgument(keyArg);
    
    receiveFiles.SetHandler(async key =>
    {
        var fileTransfer = new XdpFileTransfer(conn);

        foreach (var path in await fileTransfer.RetrieveFiles(key))
        {
            Console.WriteLine(path);
        }
    }, keyArg);

    root.AddCommand(receiveFiles);
}

{
    Command bindGlobalShortcuts = new Command("bind-global-shortcuts");

    var shortcutsArg = new Argument<string[]>("shortcuts");
    bindGlobalShortcuts.AddArgument(shortcutsArg);
    
    bindGlobalShortcuts.SetHandler(async ctx =>
    {
        var shortcuts = ctx.ParseResult.GetValueForArgument(shortcutsArg);
        
        var globalShortcuts = new XdpGlobalShortcuts(conn);
        await using var session = await globalShortcuts.CreateSession(ctx.GetCancellationToken());

        var bound = await session.BindShortcuts(
            default,
            ctx.GetCancellationToken(),
            shortcuts.Select(x => new XdpGlobalShortcuts.BindableShortcut
            {
                Id = x,
                Description = x
            })
            .ToArray()
        );

        foreach (var shortcut in bound)
        {
            Console.WriteLine($"{shortcut.Id} -> {shortcut.TriggerDescription}");

            shortcut.Activated += _ =>
            {
                Console.WriteLine($"{shortcut.Id} Activated");
            };
            
            shortcut.Deactivated += _ =>
            {
                Console.WriteLine($"{shortcut.Id} Deactivated");
            };
        }

        await Task.Delay(-1, ctx.GetCancellationToken());
    });

    root.AddCommand(bindGlobalShortcuts);
}

{
    Command trackLocation = new Command("track-location");

    trackLocation.SetHandler(new Func<InvocationContext, Task>(async ctx =>
    {
        var location = new XdpLocation(conn);
        await using var session = await location.CreateSession();

        session.LocationUpdated += loc =>
        {
            Console.WriteLine(loc.ToString());
        };
        
        await session.Start(default, ctx.GetCancellationToken());

        await Task.Delay(-1, ctx.GetCancellationToken());
    }));

    root.AddCommand(trackLocation);
}

return await root.InvokeAsync(args);