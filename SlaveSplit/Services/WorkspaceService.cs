using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SlaveSplit.Models;
using SlaveSplit.Services.GitHub;

namespace SlaveSplit.Services
{
    public sealed class WorkspaceChangedEventArgs : EventArgs { public GitHubWorkspace Workspace { get; set; } }
    public sealed class WorkspaceService
    {
        public const string DefaultCredentialId = "default-github";
        private readonly string path; private WorkspaceSettings settings; private string executionWorkspaceId;
        public event EventHandler<WorkspaceChangedEventArgs> ActiveWorkspaceChanged; public event EventHandler WorkspacesChanged;
        public WorkspaceService(string directory = null, GitHubSettings legacy = null)
        {
            string root=directory??Environment.GetEnvironmentVariable("SLAVESPLIT_DATA_DIR")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SlaveSplit"); Directory.CreateDirectory(root); path=Path.Combine(root,"workspaces.json");
            try { settings=File.Exists(path)?JsonConvert.DeserializeObject<WorkspaceSettings>(File.ReadAllText(path)):null; } catch { settings=null; }
            if(settings==null)settings=new WorkspaceSettings(); if(settings.Workspaces==null)settings.Workspaces=new List<GitHubWorkspace>(); if(settings.CredentialProfiles==null)settings.CredentialProfiles=new List<GitHubCredentialProfile>();
            if(settings.CredentialProfiles.All(x=>x.Id!=DefaultCredentialId))settings.CredentialProfiles.Add(new GitHubCredentialProfile{Id=DefaultCredentialId,Name="Default GitHub"});
            if(settings.Workspaces.Count==0){legacy=legacy??new GitHubSettings();string repo=string.IsNullOrWhiteSpace(legacy.Repository)?"SlaveSplit":legacy.Repository;settings.Workspaces.Add(new GitHubWorkspace{Id=Slug(legacy.Owner+"/"+repo),DisplayName=repo,Owner=legacy.Owner,Repository=repo,CredentialProfileId=DefaultCredentialId,ProjectId=legacy.ProjectId,IsEnabled=true});}
            if(!settings.Workspaces.Any(x=>x.Id==settings.ActiveWorkspaceId))settings.ActiveWorkspaceId=settings.Workspaces[0].Id; Save();
        }
        public WorkspaceService(IEnumerable<GitHubWorkspace> testWorkspaces,string activeWorkspaceId){path=null;settings=new WorkspaceSettings{Workspaces=(testWorkspaces??Enumerable.Empty<GitHubWorkspace>()).ToList(),ActiveWorkspaceId=activeWorkspaceId,CredentialProfiles=new List<GitHubCredentialProfile>{new GitHubCredentialProfile{Id=DefaultCredentialId,Name="Default GitHub"}}};if(settings.Workspaces.Count==0)throw new ArgumentException("At least one workspace is required.");if(!settings.Workspaces.Any(x=>x.Id==settings.ActiveWorkspaceId))settings.ActiveWorkspaceId=settings.Workspaces[0].Id;}
        public IList<GitHubWorkspace> Workspaces { get { return settings.Workspaces.Where(x=>x.IsEnabled).ToList(); } }
        public GitHubWorkspace ActiveWorkspace { get { return settings.Workspaces.First(x=>x.Id==settings.ActiveWorkspaceId); } }
        public string CurrentScopeId { get { return executionWorkspaceId??settings.ActiveWorkspaceId; } }
        public GitHubWorkspace Resolve(string value)
        {
            string text=(value??"").Trim(); List<GitHubWorkspace> exact=Workspaces.Where(x=>string.Equals(x.DisplayName,text,StringComparison.OrdinalIgnoreCase)||string.Equals(x.Repository,text,StringComparison.OrdinalIgnoreCase)).ToList(); if(exact.Count==1)return exact[0];
            List<GitHubWorkspace> partial=Workspaces.Where(x=>(x.DisplayName??"").IndexOf(text,StringComparison.OrdinalIgnoreCase)>=0||(x.Repository??"").IndexOf(text,StringComparison.OrdinalIgnoreCase)>=0).ToList(); return partial.Count==1?partial[0]:null;
        }
        public bool IsAmbiguous(string value) { string text=value??""; return Workspaces.Count(x=>(x.DisplayName??"").IndexOf(text,StringComparison.OrdinalIgnoreCase)>=0||(x.Repository??"").IndexOf(text,StringComparison.OrdinalIgnoreCase)>=0)>1; }
        public void SetActive(GitHubWorkspace workspace) { if(workspace==null||workspace.Id==settings.ActiveWorkspaceId)return;settings.ActiveWorkspaceId=workspace.Id;Save();var h=ActiveWorkspaceChanged;if(h!=null)h(this,new WorkspaceChangedEventArgs{Workspace=workspace}); }
        public IDisposable Use(string id) { string previous=executionWorkspaceId;executionWorkspaceId=id;return new Scope(()=>executionWorkspaceId=previous); }
        public void Add(GitHubWorkspace workspace){if(workspace==null)throw new ArgumentNullException("workspace");if(string.IsNullOrWhiteSpace(workspace.Id))workspace.Id=Slug(workspace.Owner+"/"+workspace.Repository);if(string.IsNullOrWhiteSpace(workspace.CredentialProfileId))workspace.CredentialProfileId=DefaultCredentialId;workspace.IsEnabled=true;settings.Workspaces.Add(workspace);Save();var h=WorkspacesChanged;if(h!=null)h(this,EventArgs.Empty);}
        private void Save(){if(string.IsNullOrWhiteSpace(path))return;string temp=path+".tmp";File.WriteAllText(temp,JsonConvert.SerializeObject(settings,Formatting.Indented));if(File.Exists(path))File.Replace(temp,path,path+".bak",true);else File.Move(temp,path);}
        private static string Slug(string value){return (value??Guid.NewGuid().ToString("N")).Trim().ToLowerInvariant().Replace(' ','-').Replace('/','-').Replace('\\','-');}
        private sealed class Scope:IDisposable{private Action close;public Scope(Action value){close=value;}public void Dispose(){Action value=close;close=null;if(value!=null)value();}}
    }
}
