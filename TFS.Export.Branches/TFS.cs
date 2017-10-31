using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace TFS.Export.Branches
{
    public class TFS
    {
        public void Go()
        {
            Get();
        }


        private void Get()
        {
            var tfs = TfsTeamProjectCollectionFactory.
                GetTeamProjectCollection(new Uri("http://URL:8080/tfs"));

           tfs.EnsureAuthenticated();

            var versionControl = tfs.GetService<VersionControlServer>();
           
            var list = versionControl.QueryRootBranchObjects(RecursionType.Full)
                .Where(b => !b.Properties.RootItem.IsDeleted)
                .Select(s => new
                {
                    Project = s.Properties.RootItem.Item
                        .Substring(0, s.Properties.RootItem.Item.IndexOf('/', 2)),
                    Properties = s.Properties,
                    DateCreated = s.DateCreated,
                    ChildBranches = s.ChildBranches
                })
                .Select(s => new BranchModel()
                {
                    ProjectName = s.Project,
                    Branch = s.Properties.RootItem.Item.Replace(s.Project, ""),
                    SourceType = "Branch",
                    Parent = s.Properties.ParentBranch != null
                        ? s.Properties.ParentBranch.Item.Replace(s.Project, "")
                        : "",
                    Version = (s.Properties.RootItem.Version as ChangesetVersionSpec)
                        .ChangesetId,
                    DateCreated = s.DateCreated,
                    Owner = s.Properties.Owner,
                    ChildBranches = s.ChildBranches
                        .Where(cb => !cb.IsDeleted)
                        .Select(cb => new BranchModel()
                        {
                            Branch = cb.Item.Replace(s.Project, ""),
                            Version = (cb.Version as ChangesetVersionSpec).ChangesetId
                        }).OrderBy(x => x.Version).ToList()
                }).ToList();
            
            var allTeamProjects = versionControl.GetAllTeamProjects(true).Select(x => new TeamProject { ProjectName = x.ServerItem }).ToList();
            foreach (var teamProject in allTeamProjects)
            {
                var brnachesAndFolders = versionControl.GetItems(new ItemSpec(
                    teamProject.ProjectName,
                    RecursionType.OneLevel
                ), VersionSpec.Latest, DeletedState.NonDeleted, ItemType.Any, GetItemsOptions.IncludeBranchInfo);

                if (brnachesAndFolders != null)
                    foreach (var item in brnachesAndFolders.Items)
                    {
                        var branchName = item.ServerItem.Replace(teamProject.ProjectName, "");
                        if (!string.IsNullOrEmpty(branchName))
                        {
                            var foundItem =
                                list.Any(x => x.Branch == branchName && x.ProjectName == teamProject.ProjectName);
                            if (foundItem)
                            {
                                teamProject.Branches.Add(list.First(x => x.Branch == branchName && x.ProjectName == teamProject.ProjectName));
                            }
                            else
                            {
                                teamProject.Branches.Add(new BranchModel
                                {
                                    Branch = item.ServerItem.Replace(teamProject.ProjectName, ""),
                                    SourceType = "Folder",
                                    DateCreated = item.CheckinDate,
                                    Version = item.ChangesetId
                                });
                            }
                        }
                    }
            }

            var path = GenerateExcel(allTeamProjects);
            Debug.WriteLine(list);
        }
        
        private string GenerateExcel(List<TeamProject> input)
        {
            
            var path = Path.Combine(@"C:\temp", Guid.NewGuid() + ".csv");
            var sb = new StringBuilder();
            using (var sw = new StreamWriter(path))
            {
                foreach (var tp in input)
                {
                    sb.AppendLine("Team Project Name : " + tp.ProjectName);
                    sb.AppendLine("Branch,Source Type, Parent,Owner,Date Created,Version,Migration Strategy");
                    foreach (var branch in tp.Branches)
                    {
                        sb.AppendLine($"{branch.Branch},{branch.SourceType},{branch.Parent},{branch.Owner},{branch.DateCreated},{branch.Version}");
                    }
                    sb.AppendLine();
                }
                sw.Write(sb.ToString());
            }
            
            return "foo";
        }
    }

    public class BranchModel
    {
        public string ProjectName { get; set; }
        public string Branch { get; set; }
        public string SourceType { get; set; }
        public string Parent { get; set; }
        public int Version { get; set; }
        public DateTime DateCreated { get; set; }
        public string Owner { get; set; }
        public List<BranchModel> ChildBranches { get; set; }
    }

    public class TeamProject
    {
        public TeamProject()
        {
            Branches = new List<BranchModel>();
        }

        public string ProjectName { get; set; }
        public IList<BranchModel> Branches { get; set; }
    }

}
