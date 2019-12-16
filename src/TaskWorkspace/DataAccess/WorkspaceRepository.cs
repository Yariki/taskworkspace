using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using Microsoft.VisualStudio.Shell.Interop;
using TaskWorkspace.Infrastructure;
using TaskWorkspace.Model;

namespace TaskWorkspace.DataAccess
{
    public class WorkspaceRepository
    {
        private readonly IVsSolution _solution;
        enum SolutionInfo
        {
            Folder,
            Name,
            UserOptionFile
        }

        public WorkspaceRepository(IVsSolution solution)
        {
            _solution = solution;

        }

        internal string SolutionFolder => GetSolutionInfo(SolutionInfo.Folder);

        internal string Filename => $"{SolutionName}.db";

        private string SolutionName
        {
            get
            {
                var name = Path.GetFileNameWithoutExtension(GetSolutionInfo(SolutionInfo.Name));
                return name;
            }
        } 

        private LiteDatabase GetDatabase()
        {
            return new LiteDatabase(Path.Combine(SolutionFolder, Filename));
        }

        public IEnumerable<string> GetWorkspaces()
        {
            using (var db = GetDatabase())
            {
                var workspaces = db.GetCollection<Workspace>();
                return workspaces.Find(o => true).Select(w => w.Name).ToList();
            }
        }

        public bool IsExist(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            using (var db = GetDatabase())
            {
                return db.GetCollection<Workspace>().Find(w => w.Name == name).Any();
            }
        }

        public Workspace GetWorkspace(string name)
        {
            using (var db = GetDatabase())
            {
                var workspace = db.GetCollection<Workspace>()
                    .Include(x => x.Breakpoints)
                    .Find(w => w.Name == name).FirstOrDefault();

                return workspace;
            }
        }

        public void SaveWorkspace(string name,List<Breakpoint> breakpoints, string windowsBase64)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            using (var db = GetDatabase())
            {
                var workspace = new Workspace
                {
                    Name = name,
                    Breakpoints = breakpoints,
                    WindowsBase64 = windowsBase64
                };

                var workspaces = db.GetCollection<Workspace>();
                if(workspaces != null)
                {
                    var result = workspaces.Insert(workspace);
                    workspaces.EnsureIndex(x => x.Name);
                }
            }
        }

        public void UpdateWorkspace(string name,List<Breakpoint> breakpoints, string windowsBase64)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            using (var db = GetDatabase())
            {
                var workspace = GetWorkspace(name);
                workspace.Breakpoints = breakpoints;
                workspace.WindowsBase64 = windowsBase64;

                var workspaces = db.GetCollection<Workspace>();
                if(workspaces != null)
                {
                    var result = workspaces.Update(workspace);
                    workspaces.EnsureIndex(x => x.Name);
                }
            }
        }

        public void DeleteWorkspace(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            using (var db = GetDatabase())
            {
                var workspaces = db.GetCollection<Workspace>();
                workspaces.Delete(x => x.Name == name);
            }
        }


        private string GetSolutionInfo(SolutionInfo type)
        {
            string solutionFolder, solutionName, optFile;
            _solution.GetSolutionInfo(out solutionFolder, out solutionName, out optFile);

            switch (type)
            {
                case SolutionInfo.Folder:
                    return solutionFolder;
                case SolutionInfo.Name:
                    return solutionName;
                case SolutionInfo.UserOptionFile:
                    return optFile;
            }
            return string.Empty;
        }

    }
}