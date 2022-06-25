using System.Text.Json;
using H = History;

List<FileInfo> GetAllFiles(string path)
{
    var result = new List<FileInfo>();
    var di = new DirectoryInfo(path);
    var stk = new Stack<DirectoryInfo>();

    stk.Push(di);

    while (stk.Count > 0)
    {
        var top = stk.Pop();
        var fs = top.GetFiles();
        foreach (var item in fs)
        {
            result.Add(item);
        }

        var ds = top.GetDirectories();
        foreach (var item in ds)
        {
            stk.Push(item);
        }
    }

    return result;
}

List<DirectoryInfo> GetAllDirectories(string path)
{
    var result = new List<DirectoryInfo>();
    var di = new DirectoryInfo(path);
    var stk = new Stack<DirectoryInfo>();

    stk.Push(di);

    while (stk.Count > 0)
    {
        var top = stk.Pop();
        var ds = top.GetDirectories();
        foreach (var item in ds)
        {
            result.Add(item);
            stk.Push(item);
        }
    }

    return result;
}

//List<H.d>

List<H.Data> WriteDirectoryInfo(string path)
{
    var dir = (from i in GetAllDirectories(path)
               let fullName = i.FullName.Substring(path.Length)
               let fileTime = i.LastWriteTime.ToLocalTime().ToFileTime()
               orderby CountChar(fullName, '\\')
               where !fullName.Contains(".history")
               select new H.Data(fullName, H.FileType.Directory, fileTime)).ToList();
    return dir;
}

List<H.Data> WriteFileInfo(string path)
{
    var data = new List<H.Data>();
    var lst = (from i in GetAllFiles(path)
               orderby CountChar(i.FullName, '\\') descending
               let fullName = i.FullName.Substring(path.Length)
               let fileTime = i.LastWriteTime.ToLocalTime().ToFileTime()
               where !fullName.Contains(".history")
               //where !fullName.Contains("\\.history")
               select new H.Data(fullName, H.FileType.File, fileTime)).ToList();
    return lst;
}

//string path = @"d:\local\dotnet\ConsoleApp\file\obj\Debug\net6.0\";
//path = @"d:\local\dotnet\ConsoleApp\file\obj\Debug\";

//WriteDirectoryInfo(path, @"D:\dir.json");
//WriteFileInfo(path, @"D:\file.json");

//if (args.)
//{

//}
if (args == null)
{
    return 1;
}

if (args != null)
{
    Console.WriteLine("{0}", args.Length);
}

if (args != null)
{
    var pathIdx = args.ToList().FindIndex(x => x == "--path");
    var listCmd = args.ToList().FindIndex(x => x == "--list");
    var removeCmd = args.ToList().FindIndex(x => x == "--remove");
    var addCmd = args.ToList().FindIndex(x => x == "--add");
    var cleanCmd = args.ToList().FindIndex(x => x == "--clean");
    var diffCmd = args.ToList().FindIndex(x => x == "--diff");
    var testCmd = args.ToList().FindIndex(x => x == "--test");
    var mergeCmd = args.ToList().FindIndex(x => x == "--merge");
    if (pathIdx == -1 || pathIdx == args.Length - 1)
    {
        return 1;
    }
    var path = args[pathIdx + 1];
    Console.WriteLine("path: {0} {1}", path, Directory.Exists(path));
    var history = Path.Combine(path, ".history");
    Console.WriteLine("history: {0} {1}", history, Directory.Exists(history));
    if (!Directory.Exists(history))
    {
        Directory.CreateDirectory(history);
    }
    //Console.WriteLine();

    if (addCmd != -1)
    {
        string name = "";
        if (addCmd != args.Length - 1)
        {
            name = args[args.Length - 1];
            Console.WriteLine("name: {0}", name);
        }
        AddCmder(path, name);
        return 0;
    }

    if (listCmd != -1)
    {
        ListCmder(history);
        return 0;
    }

    if (removeCmd != -1 || removeCmd == args.Length - 1)
    {
        RemoveCmder(history, removeCmd);
        return 0;
    }

    if (cleanCmd != -1)
    {
        CleanCmder(history);
        return 0;
    }

    if (diffCmd != -1 || removeCmd > args.Length - 2)
    {
        //CleanCmder(history);
        var newItem = args[diffCmd + 1];
        var oldItem = args[diffCmd + 2];
        Console.WriteLine("new: {0} old: {1}", newItem, oldItem);
        diffCmder(path, newItem, oldItem);
        return 0;
    }

    if (testCmd != -1)
    {
        test(path);
    }

    if (mergeCmd != -1 && mergeCmd < args.Length - 1)
    {
        //test(path)
        var mergePath = args[mergeCmd + 1];
        merge(path, mergePath);
    }
}

return 0;

void DeleteDirectory(string path)
{
    if (Directory.Exists(path))
    {
        // 備份
        Directory.Delete(path, true);
    }
}

void DeleteFile(string path)
{
    if (File.Exists(path))
    {
        File.Delete(path);
    }
}

void CopyFile(string sourceFileName, string destFileName)
{
    CreateDirectoryFor(destFileName);
    File.Copy(sourceFileName, destFileName, true);
}

void merge(string path, string mergePath)
{
    var content = File.ReadAllText(Path.Combine(mergePath, "data.json"));
    var data = JsonSerializer.Deserialize<List<H.FileInfo>>(content);
    if (data == null)
    {
        return;
    }
    foreach (var item in data)
    {
        var FullName = item.FullName;
        var info = Path.Combine(path, FullName);
        switch (item.State)
        {
            case H.FileState.Create:
            case H.FileState.Modify:
                switch (item.Type)
                {
                    case H.FileType.Directory:
                        if (Directory.Exists(info))
                        {
                            //Directory.Delete(info, true);
                        }
                        break;
                    case H.FileType.File:
                        CopyFile(Path.Combine(mergePath, FullName), info);
                        break;
                    default:
                        break;
                }
                break;
            case H.FileState.Delete:
                switch (item.Type)
                {
                    case H.FileType.File:
                        DeleteFile(info);
                        break;
                    case H.FileType.Directory:
                        DeleteDirectory(info);
                        break;
                    default:
                        break;
                }
                break;
            default:
                break;
        }
    }
}

int CountChar(string str, char c)
{
    return (from i in str where i == c select i).Count();
}

void AddCmder(string path, string name)
{
    var history = Path.Combine(path, ".history");
    var now = Path.Combine(history, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
    if (name != "")
    {
        now = Path.Combine(history, name);
    }
    Console.WriteLine("now: {0}", now);
    if (!Directory.Exists(now))
    {
        Directory.CreateDirectory(now);
    }
    var dir = WriteDirectoryInfo(path);
    var file = WriteFileInfo(path);
    dir.AddRange(file);
    File.WriteAllText(Path.Combine(now, "data.json"), JsonSerializer.Serialize(dir));
    ListCmder(history);
}

void ListCmder(string history)
{
    var currentHis = Directory.GetDirectories(history).Reverse();
    foreach (var item in currentHis)
    {
        Console.WriteLine(item);
    }
}

void RemoveCmder(string history, int removeCmd)
{
    var removeItem = args[removeCmd + 1];
    var removeHis = Path.Combine(history, removeItem);
    if (Directory.Exists(removeHis))
    {
        Directory.Delete(removeHis, true);
    }
    ListCmder(history);
}

void CleanCmder(string history)
{
    var currentHis = Directory.GetDirectories(history).Reverse();
    foreach (var item in currentHis)
    {
        //Console.WriteLine(item);
        Directory.Delete(item, true);
    }
}

void diffDir(string history, string newItem, string oldItem)
{
    var newString = File.ReadAllText(Path.Combine(history, newItem, "dir.json"));
    Console.WriteLine("newDirString: {0}", newString);
    var oldString = File.ReadAllText(Path.Combine(history, oldItem, "dir.json"));
    Console.WriteLine("oldDirString: {0}", oldString);
    var newData = JsonSerializer.Deserialize<List<string>>(newString);
    var oldData = JsonSerializer.Deserialize<List<string>>(oldString);

    if (newData == null || oldData == null)
    {
        return;
    }

    var newSet = new HashSet<string>();
    foreach (var item in newData)
    {
        newSet.Add(item);
    }

    var oldSet = new HashSet<string>();
    foreach (var item in oldData)
    {
        oldSet.Add(item);
    }

    var addDir = newSet.Except(oldSet);
    var removeDir = oldSet.Except(newSet);
    foreach (var item in addDir)
    {
        Console.WriteLine("add: {0}", item);
    }
    foreach (var item in removeDir)
    {
        Console.WriteLine("remove: {0}", item);
    }
}

List<H.FileInfo>? diffData(string history, string newItem, string oldItem)
{
    var newString = File.ReadAllText(Path.Combine(history, newItem, "data.json"));
    Console.WriteLine("newString: {0}", newString);
    var oldString = File.ReadAllText(Path.Combine(history, oldItem, "data.json"));
    Console.WriteLine("oldString: {0}", oldString);

    var newData = JsonSerializer.Deserialize<List<H.Data>>(newString);
    var oldData = JsonSerializer.Deserialize<List<H.Data>>(oldString);

    if (oldData == null || newData == null)
    {
        return null;
    }

    var oldDict = oldData.ToDictionary(i => i.FullName);
    var newDict = newData.ToDictionary(i => i.FullName);

    //var newInfo = (from i in newDict
    //               let FullName = i.Key
    //               let Type = i.Value.Type
    //               where !oldDict.ContainsKey(FullName)
    //               group i by Type
    //               //where !oldDict.ContainsKey(FullName) && Type == H.FileType.File
    //               //select new H.FileInfo(FullName, H.FileType.File, H.FileState.Create)
    //    ).ToList();

    var deleteFile = (from i in oldDict
                      let FullName = i.Key
                      let Type = i.Value.Type
                      orderby CountChar(FullName, '\\') descending // 由深到浅
                      where !newDict.ContainsKey(FullName) && Type == H.FileType.File
                      select new H.FileInfo(FullName, H.FileType.File, H.FileState.Delete)).ToList();

    var deleteDir = (from i in oldDict
                     let FullName = i.Key
                     let Type = i.Value.Type
                     orderby CountChar(FullName, '\\') descending // 由深到浅
                     where !newDict.ContainsKey(FullName) && Type == H.FileType.Directory
                     select new H.FileInfo(FullName, H.FileType.Directory, H.FileState.Delete)).ToList();

    var newFile = (from i in newDict
                   let FullName = i.Key
                   let Type = i.Value.Type
                   orderby CountChar(FullName, '\\') descending // 由深到浅
                   where !oldDict.ContainsKey(FullName) && Type == H.FileType.File
                   select new H.FileInfo(FullName, H.FileType.File, H.FileState.Create)).ToList();

    var newDir = (from i in newDict
                  let FullName = i.Key
                  let Type = i.Value.Type
                  orderby CountChar(FullName, '\\') descending // 由深到浅
                  where !oldDict.ContainsKey(FullName) && Type == H.FileType.Directory
                  select new H.FileInfo(FullName, H.FileType.Directory, H.FileState.Create)).ToList();

    var modifyFile = (from i in newDict
                      let FullName = i.Key
                      let Type = i.Value.Type
                      let FileTime = i.Value.FileTime
                      orderby CountChar(FullName, '\\') descending // 由深到浅
                      where oldDict.ContainsKey(FullName) && Type == H.FileType.File && oldDict[FullName].FileTime != FileTime
                      select new H.FileInfo(FullName, H.FileType.File, H.FileState.Modify)).ToList();

    deleteFile.AddRange(deleteDir);
    deleteFile.AddRange(newFile);
    deleteFile.AddRange(newDir);
    deleteFile.AddRange(modifyFile);

    var diff = Path.Combine(history, newItem, oldItem, "data");
    Console.WriteLine("diff: {0}", diff);
    Directory.CreateDirectory(diff);

    File.WriteAllText(Path.Combine(history, newItem, oldItem, "data.json"), JsonSerializer.Serialize(deleteFile));

    return deleteFile;
}

void diffFile(string history, string newItem, string oldItem)
{
    var newString = File.ReadAllText(Path.Combine(history, newItem, "file.json"));
    Console.WriteLine("newString: {0}", newString);
    var oldString = File.ReadAllText(Path.Combine(history, oldItem, "file.json"));
    Console.WriteLine("oldString: {0}", oldString);
    var newData = JsonSerializer.Deserialize<List<H.Data>>(newString);
    var oldData = JsonSerializer.Deserialize<List<H.Data>>(oldString);

    if (newData == null || oldData == null)
    {
        return;
    }

    var newDict = new Dictionary<string, long>();
    foreach (var item in newData)
    {
        newDict[item.FullName] = item.FileTime;
    }

    var oldDict = new Dictionary<string, long>();
    foreach (var item in oldData)
    {
        oldDict[item.FullName] = item.FileTime;
    }

    var removePart = from i in oldData
                     where !newDict.ContainsKey(i.FullName)
                     select new H.FileInfo(i.FullName, H.FileType.File, H.FileState.Delete);

    var addPart = from i in newData
                  where !oldDict.ContainsKey(i.FullName)
                  select new H.FileInfo(i.FullName, H.FileType.File, H.FileState.Create);

    var modifyPart = from i in newData
                     where (oldDict.ContainsKey(i.FullName) && oldDict[i.FullName] != i.FileTime)
                     select new H.FileInfo(i.FullName, H.FileType.File, H.FileState.Modify);

    var result = removePart.Union(addPart).Union(modifyPart).ToList();

    var diff = Path.Combine(history, newItem, oldItem);
    Console.WriteLine("diff: {0}", diff);
    Directory.CreateDirectory(diff);

    File.WriteAllText(Path.Combine(history, newItem, oldItem, "file.json"), JsonSerializer.Serialize(result));
}

void diffCmder(string path, string newItem, string oldItem)
{
    var history = Path.Combine(path, ".history");
    //diffDir(history, newItem, oldItem);
    //diffFile(history, newItem, oldItem);
    var result = diffData(history, newItem, oldItem);
    if (result != null)
    {
        var diff = Path.Combine(history, newItem, oldItem, "data");
        foreach (var item in result)
        {
            if (item.Type == H.FileType.File &&
                (item.State == H.FileState.Create || item.State == H.FileState.Modify))
            {
                var fullPath = Path.Combine(path, item.FullName);
                Console.WriteLine("file: {0} {1}", item.FullName,
                    fullPath);
                // 複製出來
                CreateDirectoryFor(Path.Combine(diff, item.FullName));
                File.Copy(fullPath, Path.Combine(diff, item.FullName), true);
            }
        }
    }
}

//DirectoryInfo? GetDirectory(string path)
//{
//    return new FileInfo(path).Directory;
//}

void CreateDirectoryFor(string path)
{
    var dir = new FileInfo(path).Directory;
    if (dir != null && !dir.Exists)
    {
        dir.Create();
    }
}

//void Copy(string sourceFileName, string destFileName, bool overwrite)
//{
//    var dir = new FileInfo(destFileName).Directory;
//    if (dir != null && !dir.Exists)
//    {
//        dir.Create();
//    }
//    File.Copy(sourceFileName, destFileName, overwrite);
//}

void test(string path)
{
    //if (Directory.Exists(path))
    //{
    //    Directory.Delete(path, true);
    //}

    //Directory.CreateDirectory(Path.Combine(path, "1/2"));

    //File.WriteAllText(Path.Combine(path, "1.txt"), "1");
    //File.WriteAllText(Path.Combine(path, "1/2", "2.txt"), "2");

    // add a
    // updata file
    // add b
    // diff b a
    // merge b a

    // 複製文件夾全部內容到此處
    // add a

}

namespace History
{
    record struct Data(string FullName, FileType Type, long FileTime);
    enum FileType
    {
        Directory,
        File
    }
    enum FileState
    {
        Create,
        Modify,
        Delete
    }
    record struct FileInfo(string FullName, FileType Type, FileState State);
}
