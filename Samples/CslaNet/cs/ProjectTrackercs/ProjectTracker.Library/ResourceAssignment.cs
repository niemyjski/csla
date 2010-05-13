using Csla;
using System;
using Csla.Serialization;

namespace ProjectTracker.Library
{
  [Serializable()]
  public class ResourceAssignment : BusinessBase<ResourceAssignment>, IHoldRoles
  {
    #region  Business Methods

    private static PropertyInfo<byte[]> TimeStampProperty = RegisterProperty<byte[]>(c => c.TimeStamp);
    private byte[] TimeStamp
    {
      get { return GetProperty(TimeStampProperty); }
      set { SetProperty(TimeStampProperty, value); }
    }

    private static PropertyInfo<Guid> ProjectIdProperty = 
      RegisterProperty<Guid>(r=>r.ProjectId, "Project id", Guid.Empty);
    public Guid ProjectId
    {
      get { return GetProperty(ProjectIdProperty); }
      private set { SetProperty(ProjectIdProperty, value); }
    }

    private static PropertyInfo<string> ProjectNameProperty = 
      RegisterProperty<string>(r=>r.ProjectName);
    public string ProjectName
    {
      get { return GetProperty(ProjectNameProperty); }
      private set { SetProperty(ProjectNameProperty, value); }
    }

    private static PropertyInfo<SmartDate> AssignedProperty = 
      RegisterProperty<SmartDate>(r=>r.Assigned);
    public string Assigned
    {
      get { return GetPropertyConvert<SmartDate, string>(AssignedProperty); }
    }

    private static PropertyInfo<int> RoleProperty = RegisterProperty<int>(r=>r.Role);
    public int Role
    {
      get { return GetProperty(RoleProperty); }
      set { SetProperty(RoleProperty, value); }
    }

    //public static readonly MethodInfo GetProjectMethod = RegisterMethod(typeof(ResourceAssignment), "GetProject");
    //public Project GetProject()
    //{
    //  CanExecuteMethod(GetProjectMethod, true);
    //  return Project.GetProject(ProjectId);
    //}

    public override string ToString()
    {
      return ProjectId.ToString();
    }

    #endregion

    #region  Business Rules

    protected override void AddBusinessRules()
    {
      BusinessRules.AddRule(new Assignment.ValidRole { PrimaryProperty = RoleProperty });

      BusinessRules.AddRule(new Csla.Rules.CommonRules.IsInRole(Csla.Rules.AuthorizationActions.WriteProperty, RoleProperty, "ProjectManager"));
    }

    #endregion

#if SILVERLIGHT
    internal static void NewResourceAssignment(Guid projectId, Action<ResourceAssignment> callback)
    {
      DataPortal.BeginExecute<ResourceAssignmentCreator>(new ResourceAssignmentCreator { ProjectId = projectId }, (o, e) =>
        {
          callback(e.Object.Result);
        });
    }

    [Serializable]
    private class ResourceAssignmentCreator : CommandBase<ResourceAssignmentCreator>
    {
      public static PropertyInfo<Guid> ProjectIdProperty = RegisterProperty<Guid>(c => c.ProjectId);
      public Guid ProjectId
      {
        get { return ReadProperty(ProjectIdProperty); }
        set { LoadProperty(ProjectIdProperty, value); }
      }

      public static PropertyInfo<ResourceAssignment> ResultProperty = RegisterProperty<ResourceAssignment>(c => c.Result);
      public ResourceAssignment Result
      {
        get { return ReadProperty(ResultProperty); }
        set { LoadProperty(ResultProperty, value); }
      }
#if !SILVERLIGHT
      protected override void DataPortal_Execute()
      {
        Result = ResourceAssignment.NewResourceAssignment(projectId);
      }
#endif
    }

#else
    #region  Factory Methods

    internal static ResourceAssignment NewResourceAssignment(Guid projectId)
    {
      return DataPortal.CreateChild<ResourceAssignment>(projectId, RoleList.DefaultRole());
    }

    internal static ResourceAssignment GetResourceAssignment(ProjectTracker.DalLinq.Assignment data)
    {
      return DataPortal.FetchChild<ResourceAssignment>(data);
    }

    private ResourceAssignment()
    { /* require use of factory methods */ }

    #endregion

    #region  Data Access

    private void Child_Create(Guid projectId, int role)
    {
      var proj = Project.GetProject(projectId);
      using (BypassPropertyChecks)
      {
        ProjectId = proj.Id;
        ProjectName = proj.Name;
        LoadPropertyConvert<SmartDate, DateTime>(AssignedProperty, Assignment.GetDefaultAssignedDate());
        Role = role;
      }
    }

    private void Child_Fetch(ProjectTracker.DalLinq.Assignment data)
    {
      using (BypassPropertyChecks)
      {
        ProjectId = data.ProjectId;
        ProjectName = data.Project.Name;
        LoadPropertyConvert<SmartDate, DateTime>(AssignedProperty, data.Assigned);
        Role = data.Role;
        TimeStamp = data.LastChanged.ToArray();
      }
    }

    private void Child_Insert(Resource resource)
    {
      using (BypassPropertyChecks)
      {
        TimeStamp = Assignment.AddAssignment(
          ProjectId, resource.Id, ReadProperty(AssignedProperty), Role);
      }
    }

    private void Child_Update(Resource resource)
    {
      using (BypassPropertyChecks)
      {
        TimeStamp = Assignment.UpdateAssignment(
          ProjectId, resource.Id, ReadProperty(AssignedProperty), Role, TimeStamp);
      }
    }

    private void Child_DeleteSelf(Resource resource)
    {
      using (BypassPropertyChecks)
      {
        Assignment.RemoveAssignment(ProjectId, resource.Id);
      }
    }

    #endregion
#endif
  }
}