using System.Text;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace XdgDesktopPortal.Services;

// TODO: finish this.

public class XdpFileChooser(Connection dbusConnection)
{
    internal readonly OrgFreedesktopPortalFileChooser Wrapped = new(dbusConnection, XdpUtils.Destination, XdpUtils.DesktopObject);
    internal readonly Connection dbusConnection = dbusConnection;

    public OpenFileRequest OpenFile(WindowId parentWindow, string title) => new OpenFileRequest(this, parentWindow, title);

    public abstract class OpenSaveFileRequest<TThis> where TThis : OpenSaveFileRequest<TThis>
    {
        internal readonly WindowId parentWindow;
        internal readonly string title;

        internal string? acceptLabel;
        internal bool? modal;

        internal readonly List<FileFilter> filters = [];
        internal FileFilter? currentFilter;
        internal readonly List<(string Id, string Label, List<(string Id, string Label)> Choices, string InitialSelection)> choices = [];

        internal byte[]? currentFolder;

        internal OpenSaveFileRequest(WindowId parentWindow, string title)
        {
            this.parentWindow = parentWindow;
            this.title = title;
        }

        public TThis AcceptLabel(string acceptLabel)
        {
            this.acceptLabel = acceptLabel;

            return (TThis)this;
        }

        public TThis Modal(bool modal = true)
        {
            this.modal = modal;

            return (TThis)this;
        }

        public TThis Filter(FileFilter filter, bool current = false)
        {
            if (currentFilter != null && filters.Count == 0)
                throw new InvalidOperationException("Tried to add a filter while one was already forced");

            filters.Add(filter);

            if (current)
            {
                if (currentFilter != null) throw new InvalidOperationException("Tried to set two current filters");
                currentFilter = filter;
            }

            return (TThis)this;
        }

        public TThis ForceFilter(FileFilter filter)
        {
            if (filters.Count > 0) throw new InvalidOperationException("Tried to force a filter with filter choices already present");

            currentFilter = filter;

            return (TThis)this;
        }

        public TThis Choice(string id, string label)
        {
            choices.Add((id, label, [], ""));
            return (TThis)this;
        }

        public TThis Choice(string id, string label, Dictionary<string, string> variants, string initial = "")
        {
            choices.Add((id, label, variants.Select(x => (x.Key, x.Value)).ToList(), initial));
            return (TThis)this;
        }

        public TThis CurrentFolder(string path)
        {
            currentFolder = Encoding.UTF8.GetBytes(path);
            return (TThis)this;
        }

        internal void WriteOptions(Dictionary<string, Variant> options)
        {
            if (acceptLabel != null) options["accept_label"] = acceptLabel;
            if (modal != null) options["modal"] = modal.Value;

            if (filters.Count > 0)
                options["filters"] = new Tmds.DBus.Protocol.Array<Struct<string, Tmds.DBus.Protocol.Array<Struct<uint, string>>>>(filters.Select(x => x.ToDBus()));

            if (currentFilter != null)
                options["current_filter"] = currentFilter.ToDBus();

            if (choices.Count > 0)
                options["choices"] = new Tmds.DBus.Protocol.Array<Struct<string, string, Tmds.DBus.Protocol.Array<Struct<string, string>>, string>>(choices.Select(x =>
                {
                    // (ssa(ss)s)

                    Tmds.DBus.Protocol.Array<Struct<string, string>> variants = new(x.Choices.Select(y => new Struct<string, string>(y.Id, y.Label)));

                    return new Struct<string, string, Tmds.DBus.Protocol.Array<Struct<string, string>>, string>(
                        x.Id,
                        x.Label,
                        variants,
                        x.InitialSelection
                    );
                }));

            if (currentFolder != null) options["current_folder"] = new Tmds.DBus.Protocol.Array<byte>(currentFolder);
        }

        public abstract Task<OpenSaveFileResult> Send(CancellationToken cancellationToken = default);
    }

    public sealed class OpenFileRequest : OpenSaveFileRequest<OpenFileRequest>
    {
        private readonly XdpFileChooser parent;
        internal bool? multiple;
        internal bool? directory;

        internal OpenFileRequest(XdpFileChooser parent, WindowId parentWindow, string title) : base(parentWindow, title)
        {
            this.parent = parent;
        }

        public OpenFileRequest Multiple(bool multiple = true)
        {
            this.multiple = multiple;

            return this;
        }

        public OpenFileRequest Directory(bool directory = true)
        {
            this.directory = directory;

            return this;
        }

        public override async Task<OpenSaveFileResult> Send(CancellationToken cancellationToken = default)
        {
            await using XdpRequest req = await XdpRequest.Create(parent.dbusConnection);

            Dictionary<string, Variant> options = new()
            {
                ["handle_token"] = req.HandleToken
            };

            WriteOptions(options);

            if (multiple != null) options["multiple"] = multiple.Value;
            if (directory != null) options["directory"] = directory.Value;

            req.ValidatePath(await parent.Wrapped.OpenFileAsync(parentWindow.Value, title, options));

            return OpenSaveFileResult.FromDBus(await req.Await(cancellationToken));
        }
    }

    public record struct OpenSaveFileResult(IList<Uri> Uris, Dictionary<string, string>? Choices, FileFilter? CurrentFilter)
    {
        internal static OpenSaveFileResult FromDBus(Dictionary<string, VariantValue> dict)
        {
            var uris = dict["uris"].GetArray<string>().Select(x => new Uri(x)).ToArray();

            Dictionary<string, string>? choices = null;
            FileFilter? currentFilter = null;

            if (dict.TryGetValue("choices", out VariantValue choicesDbus))
            {
                choices = choicesDbus.GetArray<VariantValue>().ToDictionary(x => x.GetItem(0).GetString(), x => x.GetItem(1).GetString());
            }

            if (dict.TryGetValue("current_filter", out VariantValue currentFilterDbus))
                currentFilter = FileFilter.FromDBus(currentFilterDbus);

            return new OpenSaveFileResult(uris, choices, currentFilter);
        }
    }

    public class FileFilter
    {
        public required string Name { get; set; }

        public List<string> GlobFilters { get; set; } = [];

        public List<string> MimeTypeFilters { get; set; } = [];

        // (sa(us))
        internal Struct<string, Tmds.DBus.Protocol.Array<Struct<uint, string>>> ToDBus()
        {
            Tmds.DBus.Protocol.Array<Struct<uint, string>> filters = new();

            foreach (string glob in GlobFilters)
            {
                filters.Add(new(0, glob));
            }

            foreach (string mime in MimeTypeFilters)
            {
                filters.Add(new(1, mime));
            }

            return new Struct<string, Tmds.DBus.Protocol.Array<Struct<uint, string>>>(Name, filters);
        }

        internal static FileFilter FromDBus(VariantValue variant)
        {
            string name = variant.GetItem(0).GetString();

            VariantValue[] filters = variant.GetItem(1).GetArray<VariantValue>();

            List<string> globFilters = [];
            List<string> mimeTypeFilters = [];

            foreach (var filter in filters)
            {
                uint type = filter.GetItem(0).GetUInt32();

                if (type == 0)
                {
                    globFilters.Add(filter.GetItem(1).GetString());
                }
                else if (type == 1)
                {
                    mimeTypeFilters.Add(filter.GetItem(1).GetString());
                }
            }

            return new FileFilter()
            {
                Name = name,
                GlobFilters = globFilters,
                MimeTypeFilters = mimeTypeFilters
            };
        }

        public override string ToString()
        {
            var allFilters = string.Join(", ", GlobFilters.Concat(MimeTypeFilters));

            return $"{Name} ({allFilters})";
        }
    }
}