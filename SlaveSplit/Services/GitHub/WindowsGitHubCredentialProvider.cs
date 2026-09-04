using System;
using System.Runtime.InteropServices;
namespace SlaveSplit.Services.GitHub
{
    public sealed class WindowsGitHubCredentialProvider : IGitHubCredentialProvider
    {
        private const string Target = "SlaveSplit/GitHub"; private const int Generic = 1; private const int PersistLocalMachine = 2;
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct Credential { public int Flags; public int Type; public string TargetName; public string Comment; public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten; public int CredentialBlobSize; public IntPtr CredentialBlob; public int Persist; public int AttributeCount; public IntPtr Attributes; public string TargetAlias; public string UserName; }
        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CredWrite(ref Credential credential, int flags);
        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CredRead(string target, int type, int flags, out IntPtr credential);
        [DllImport("advapi32.dll", EntryPoint = "CredFree", SetLastError = true)] private static extern void CredFree(IntPtr credential);
        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CredDelete(string target, int type, int flags);
        public string GetToken() { IntPtr pointer; if (!CredRead(Target, Generic, 0, out pointer)) return null; try { Credential value = (Credential)Marshal.PtrToStructure(pointer, typeof(Credential)); return value.CredentialBlob == IntPtr.Zero ? null : Marshal.PtrToStringUni(value.CredentialBlob, value.CredentialBlobSize / 2); } catch { return null; } finally { CredFree(pointer); } }
        public bool HasToken() { IntPtr pointer; try { if (!CredRead(Target, Generic, 0, out pointer)) return false; try { Credential value = (Credential)Marshal.PtrToStructure(pointer, typeof(Credential)); return value.CredentialBlob != IntPtr.Zero && value.CredentialBlobSize > 0; } finally { CredFree(pointer); } } catch { return false; } }
        public bool SaveToken(string token) { if (string.IsNullOrWhiteSpace(token)) return false; IntPtr blob = IntPtr.Zero; try { byte[] bytes = System.Text.Encoding.Unicode.GetBytes(token); blob = Marshal.AllocCoTaskMem(bytes.Length); Marshal.Copy(bytes, 0, blob, bytes.Length); Credential value = new Credential { Type = Generic, TargetName = Target, CredentialBlobSize = bytes.Length, CredentialBlob = blob, Persist = PersistLocalMachine, UserName = "GitHub Token" }; return CredWrite(ref value, 0); } catch { return false; } finally { if (blob != IntPtr.Zero) Marshal.FreeCoTaskMem(blob); } }
        public bool DeleteToken() { try { return CredDelete(Target, Generic, 0) || Marshal.GetLastWin32Error() == 1168; } catch { return false; } }
    }
}
