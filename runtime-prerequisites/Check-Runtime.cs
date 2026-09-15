using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.Win32;

// Reads metadata and hashes only; never loads a candidate DLL for execution.
class RuntimeCheck
{
    static readonly string[] Names = {
        "Microsoft.DirectX", "Microsoft.DirectX.Direct3D", "Microsoft.DirectX.Direct3DX"
    };
    static readonly string[][] Hashes = {
        new[] { "942e98f142373547493f13b14e1603b2420851aff013d3085bada7b6b2214d9c" },
        new[] { "f3359d5e41b1d4fec7230579a593e40fe44f6afdfacd1e2bbe52ee06d84686fb", "4fb206fa4cdcd0e93ddf2f926c0a8a325e5e50dd2d3353066c6885019499b173" },
        new[] { "7986e3fbe05418fe5d8425f2f1b76b7a7b09952f3ec560b286dd744bf7178059", "a33bf14230389abb0d44f8eeb88272566def3599b0c5b2f4559f3c4e29ff97a4" }
    };

    static bool Installed(string subkey)
    {
        using (RegistryKey machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
        using (RegistryKey key = machine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\" + subkey))
            return key != null && Convert.ToInt32(key.GetValue("Install", 0)) == 1;
    }

    static bool Matches(string file, int index)
    {
        try
        {
            string hash;
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(file))
                hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            if (Array.IndexOf(Hashes[index], hash) < 0) return false;
            AssemblyName name = AssemblyName.GetAssemblyName(file);
            return name.Name == Names[index] && name.Version.ToString() == "1.0.2902.0"
                && BitConverter.ToString(name.GetPublicKeyToken()).Replace("-", "").ToLowerInvariant() == "31bf3856ad364e35"
                && (name.CultureInfo == null || name.CultureInfo.Name == "");
        }
        catch (Exception error)
        {
            Console.WriteLine("  Cannot inspect candidate: " + error.GetType().Name);
            return false;
        }
    }

    static bool Find(int index)
    {
        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string[] roots = { @"assembly\GAC", @"assembly\GAC_32", @"assembly\GAC_MSIL",
            @"Microsoft.NET\assembly\GAC_32", @"Microsoft.NET\assembly\GAC_MSIL",
            @"Microsoft.NET\DirectX for Managed Code", @"Microsoft.NET\DirectX for ManagedCode" };
        int candidates = 0;
        foreach (string relative in roots)
        {
            string root = Path.Combine(windows, relative);
            if (relative.IndexOf("DirectX for", StringComparison.Ordinal) < 0)
                root = Path.Combine(root, Names[index]);
            if (!Directory.Exists(root)) continue;
            foreach (string version in Directory.GetDirectories(root))
            {
                string file = Path.Combine(version, Names[index] + ".dll");
                if (!File.Exists(file)) continue;
                candidates++;
                if (Matches(file, index))
                {
                    Console.WriteLine("PASS " + Names[index] + " 1.0.2902.0 (tested hash)");
                    return true;
                }
            }
        }
        Console.WriteLine("FAIL " + Names[index] + (candidates == 0 ? ": not found" : ": no matching tested identity/hash"));
        return false;
    }

    static int Main()
    {
        try
        {
            Console.WriteLine("Magic 600 Cell - read-only prerequisite check");
            Console.WriteLine("Checks known installation folders, not application-local caches.");
            string architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432")
                ?? Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") ?? "unknown";
            bool architectureOK = String.Equals(architecture, "AMD64", StringComparison.OrdinalIgnoreCase);
            Console.WriteLine((architectureOK ? "PASS " : "UNVERIFIED ") + "OS architecture: " + architecture + "; checker process: x86");
            bool net4 = Installed(@"v4\Full");
            bool net35 = Installed("v3.5");
            Console.WriteLine((net4 ? "PASS " : "FAIL ") + ".NET Framework 4.x Full (current native host)");
            Console.WriteLine((net35 ? "PASS " : "ATTENTION ") + ".NET Framework 3.5 (legacy compatibility prerequisite)");
            bool mdx = true;
            for (int i = 0; i < Names.Length; i++) if (!Find(i)) mdx = false;
            Console.WriteLine("This does not test GPU rendering, a clean Windows installation, or G1/G2 approval.");
            if (architectureOK && net4 && net35 && mdx)
            {
                Console.WriteLine("Prerequisite checks passed. Launch the application separately to verify operation.");
                return 0;
            }
            Console.WriteLine("See README.md. Missing .NET 3.5 alone does not prove the CLR4 host cannot run.");
            return 2;
        }
        catch (Exception error)
        {
            Console.WriteLine("Check incomplete: " + error.GetType().Name + ". No system changes were made.");
            return 3;
        }
    }
}
