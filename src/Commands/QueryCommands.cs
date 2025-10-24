using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQLClient.Commands
{
    internal static class QueryCommands
    {
        public static int PageSize { get; set; } = 20;
        public const string Query_Hubs = @"query GetHubs { hubs { pagination { cursor } results { name id }}}";

        public const string Query_Projects =
            @"query GetACCProjects($hubId: ID!) 
            { 
                projects(hubId: $hubId) 
                { 
                    pagination { pageSize cursor } 
                    results { id name alternativeIdentifiers { dataManagementAPIProjectId }}
                }
            }";
        
        public const string Query_FolderByProject = 
            @"query GetFoldersByProject($projectId: ID!, $filter: FolderFilterInput, $pagination: PaginationInput) 
            {
                foldersByProject (projectId: $projectId filter: $filter pagination: $pagination)
                {
                    pagination { pageSize cursor}
                    results { name id parentFolder { name id }}
                }
            }";

        public const string Query_SpecialFolder =
            @"query GetFoldersByFolder($projectId: ID!, $folderId: ID!, $filter: FolderFilterInput, $pagination: PaginationInput) 
            {
                foldersByFolder(projectId: $projectId  folderId: $folderId filter: $filter pagination: $pagination) 
                {
                    pagination { pageSize cursor }
                    results { name id parentFolder { name id }}
                }
            }";

        public const string Query_ElementGroup =
            @"query GetElementGroupsByFolder($projectId: ID!, $folderId: ID!,
              $filter:ElementGroupFilterInput, $pagination: PaginationInput) 
            {
                elementGroupsByFolder(projectId: $projectId folderId: $folderId filter: $filter pagination: $pagination) 
                {
                    pagination { pageSize cursor }
                    results { name id alternativeIdentifiers { fileUrn fileVersionUrn }}
                }
            }";

        public const string Query_SearchElements =
            @"query GetElementsByElementGroup($groupId: ID!, $filter: ElementFilterInput, $pagination: PaginationInput) 
            {
                elementsByElementGroup(elementGroupId: $groupId filter: $filter pagination: $pagination) 
                {
                    pagination { pageSize cursor }
                    results 
                    { 
                        name 
                        id 
                        properties 
                        {   
                            results { name displayValue definition { id name }}
                        }
                        alternativeIdentifiers { externalElementId }
                        lastModifiedOn
                        lastModifiedBy { userName id }
                    }
                }
            }";

        private static Tuple<string, string> _basicVariables;
        public static Tuple<string, string> BasicVariables
        {
            get
            {
                if (_basicVariables == null)
                {
                    _basicVariables = new Tuple<string, string>("limit", PageSize.ToString());
                }
                return _basicVariables;
            }
        }
    }
}
