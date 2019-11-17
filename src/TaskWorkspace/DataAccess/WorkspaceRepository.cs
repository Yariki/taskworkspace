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
        private readonly string _dbName = "_workspaces.db";
        private readonly IVsSolution _solution;

        public WorkspaceRepository(IVsSolution solution)
        {
            _solution = solution;

            //Init();
        }

        private string SolutionFolder
        {
            get
            {
                string solutionFolder, solutionName, optFile;
                _solution.GetSolutionInfo(out solutionFolder, out solutionName, out optFile);

                return solutionFolder;
            }
        }

        private void Init()
        {
            var mapper = BsonMapper.Global;
            mapper.Entity<Workspace>()
                .DbRef(x => x.Documents, "documents")
                .DbRef(x => x.Breakpoints, "breakpoints");
        }

        private LiteDatabase GetDatabase(string folder)
        {
            return new LiteDatabase(Path.Combine(folder, _dbName));
        }

        public IEnumerable<string> GetWorkspaces()
        {
            using (var db = GetDatabase(SolutionFolder))
            {
                var workspaces = db.GetCollection<Workspace>();
                return workspaces.Find(o => true).Select(w => w.Name).ToList();
            }
        }

        public bool IsExist(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            using (var db = GetDatabase(SolutionFolder))
            {
                return db.GetCollection<Workspace>().Find(w => w.Name == name).Any();
            }
        }

        public Workspace GetWorkspace(string name)
        {
            using (var db = GetDatabase(SolutionFolder))
            {
                var workspace = db.GetCollection<Workspace>()
                    .Include(x => x.Documents)
                    .Include(x => x.Breakpoints)
                    .Find(w => w.Name == name).FirstOrDefault();

                return workspace;
            }
        }

        public void SaveWorkspace(string name, List<Document> documents, List<Breakpoint> breakpoints)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            using (var db = GetDatabase(SolutionFolder))
            {
                var workspace = new Workspace
                {
                    Name = name,
                    Documents = documents,
                    Breakpoints = breakpoints
                };

                var workspaces = db.GetCollection<Workspace>();
                if(workspaces != null)
                {
                    var result = workspaces.Insert(workspace);
                    workspaces.EnsureIndex(x => x.Name);
                }
            }
        }

        public void UpdateWorkspace(string name,List<Document> documents,List<Breakpoint> breakpoints)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            using (var db = GetDatabase(SolutionFolder))
            {
                var workspace = GetWorkspace(name);
                workspace.Documents = documents;
                workspace.Breakpoints = breakpoints;

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

            using (var db = GetDatabase(SolutionFolder))
            {
                var workspaces = db.GetCollection<Workspace>();
                workspaces.Delete(x => x.Name == name);
            }
        }
    }
}