using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using System.Web.Services;
using AjaxControlToolkit;
using System.IO;
using System.Web.UI.HtmlControls;
using CollageProjects;
using System.Collections;
using System.Web.Hosting;
using System.Data;
using System.Data.SqlClient;
using Collage.Admin;
using Collage;
using System.Data;
using System.Data.SqlClient;
using System.Web.Script.Serialization;

namespace CollageAgency.Admin
{
    public partial class Agency : System.Web.UI.Page
    {
        Projects.ClsProjectInformation ObjProjectInformation = new Projects.ClsProjectInformation();

        Collage.Collage.ClsUser ObjUser = new Collage.Collage.ClsUser();
        Collage.Collage.ClsUserPrp ObjUserPrp = new Collage.Collage.ClsUserPrp();

        GenerateReportsListing objgeneratReportsListing = new GenerateReportsListing();

        CollageProjects.Projects.RolesJSONCLS data = new Projects.RolesJSONCLS();

        FullGridPager fullGridPagerObj;
        protected Hashtable fullGridPager = new Hashtable();
        int MaxVisible = 5;
        public int pagesize = Convert.ToInt32(ConfigurationSettings.AppSettings["MainPageSize"].ToString());

        protected void Page_Load(object sender, EventArgs e)
        {
            var grdList = ControlExtensions.GetAllControlsOfType<GridView>(this);
            if (Page.IsPostBack == false)
            {
                BindGridProjectListing(1);
            }
            else
            {
                foreach (GridView grid1 in grdList)
                {
                    fullGridPagerObj = new FullGridPager(grid1, MaxVisible, "Page", "of");
                    fullGridPagerObj.CreateCustomPager(grid1.BottomPagerRow);
                    fullGridPager[grid1.ClientID] = fullGridPagerObj;
                }
            }

        }

        protected void Page_LoadComplete(object sender, EventArgs e)
        {
            if (Session["RolesJSON"] != null)
            {
                JavaScriptSerializer jss = new JavaScriptSerializer();
                data = jss.Deserialize<CollageProjects.Projects.RolesJSONCLS>(Session["RolesJSON"].ToString());
                Permissions();
            }
        }
        public void Permissions()
        {
            common.ApplyPermissions(data.RolesJason[0].AgencyManagement[0].ProjectListing[0].Permissions[0], ProjectListingMain);
        }

        protected void txtSearch_TextChanged(object sender, EventArgs e)
        {
            BindGridProjectListing(1);
        }
        protected void GetProjectSearch(object sender, EventArgs e)
        {
            BindGridProjectListing(1);
        }

        #region Pager
        private void PopulatePager(int recordCount, int currentPage, DataList Pager)
        {
            double dblPageCount = (double)((decimal)recordCount / Convert.ToInt32(ConfigurationManager.AppSettings["PageSize"]));
            int pageCount = (int)Math.Ceiling(dblPageCount);
            List<ListItem> pages = new List<ListItem>();
            if (pageCount > 0)
            {
                pages.Add(new ListItem("&laquo;", "1", currentPage > 1));
                for (int i = 1; i <= pageCount; i++)
                {
                    pages.Add(new ListItem(i.ToString(), i.ToString(), i != currentPage));
                }
                pages.Add(new ListItem("&raquo;", pageCount.ToString(), currentPage < pageCount));
            }
            Pager.DataSource = pages;
            Pager.DataBind();
        }
        #endregion Pager

        #region ProjectListing

        private void BindGridProjectListing(int PageIndex)
        {
            string ProjectIds = "";
            //string RSN = Session["RoleShortName"].ToString();
            if (Convert.ToString(Session["RoleShortName"]) == "EC" || Convert.ToString(Session["RoleShortName"]) == "EPM" || Convert.ToString(Session["RoleShortName"]) == "EU")
            {
                List<Collage.Collage.ClsUserPrp> obj = ObjUser.GetProjectIdsByUserId(Convert.ToInt32(Session["UserId"]));
                ProjectIds = obj[0].ProjectIds.ToString();
                if (ProjectIds == "")
                {
                    ProjectIds = "0";
                }
            }

            List<Projects.ClsProjectInformationPrp> data = ObjProjectInformation.GetAgencyProjects(txtSearch.Text.Trim(), ProjectIds);
            DataTable dt = ListToDataTable.ToDataTable(data);
            dt = common.sortDatatable(dt, grdProjectListing, 2);
            ViewState[grdProjectListing.ClientID + "tblData"] = dt;
            grdProjectListing.DataSource = dt;
            grdProjectListing.DataBind();
        }
        protected void Page_ChangedProjectListing(object sender, EventArgs e)
        {
            int pageIndex = int.Parse((sender as LinkButton).CommandArgument);
            this.BindGridProjectListing(pageIndex);
        }
        protected void grdProjectListing_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "EditModule")
            {
                string[] ar = e.CommandArgument.ToString().Split(',');
                if (ar.Length > 0)
                {
                    Session["Mode"] = "Edit";
                    Session["ProjectId"] = ar.GetValue(0).ToString();
                    Response.Redirect("AgencyManagement.aspx");
                }
            }
            if (e.CommandName == "ViewModule")
            {
                string[] ar = e.CommandArgument.ToString().Split(',');
                if (ar.Length > 0)
                {
                    Session["Mode"] = "View";
                    Session["ProjectId"] = ar.GetValue(0).ToString();
                    Response.Redirect("AgencyManagement.aspx");
                }
            }
            if (e.CommandName == "DeleteModule")
            {
                int ProjectId = Convert.ToInt32(e.CommandArgument);
                if (ProjectId > 0)
                {
                    int retval = ObjProjectInformation.DeleteProject(ProjectId);
                    if (retval == 1)
                    {
                        common.showmessage(this, "msgaddnewproject", Message.Success("Project deleted successfully"));
                        BindGridProjectListing(1);
                    }
                }
            }
        }
        protected void grid_Sorting(object sender, GridViewSortEventArgs e)
        {
            GridView grid1 = (GridView)sender;
            string sortingDirection = string.Empty;
            string SortDirection = grid1.ClientID + "SortDirection";
            string SortExpression = grid1.ClientID + "SortExpression";
            string imgDir = common.getSorImage(ViewState[SortDirection]);
            DataTable dt = CollageProjects.common.getSortedDataTable(((DataTable)ViewState[grid1.ClientID + "tblData"]), (GridView)sender, ViewState[SortDirection], ViewState[SortExpression], e.SortExpression);
            ViewState[SortDirection] = CollageProjects.common.bindgridglobal(((DataTable)ViewState[grid1.ClientID + "tblData"]), (GridView)sender, ViewState[SortDirection], ViewState[SortExpression], e.SortExpression);
            ViewState[grid1.ClientID + "tblData"] = dt;
            ViewState[SortExpression] = e.SortExpression;
            foreach (DataControlField field in grid1.Columns)
            {
                if (field.SortExpression == e.SortExpression)
                {
                    string headertext = field.HeaderText;
                    if (headertext.IndexOf("<img") > 0)
                    {
                        headertext = headertext.Substring(0, headertext.IndexOf("<img"));
                    }
                    grid1.Columns[grid1.Columns.IndexOf(field)].HeaderText = headertext + imgDir;
                }
            }
            grid1.DataBind();

            hdnSortExpression.Value = e.SortExpression.ToString();
            hdnsortingascdesc.Value = Convert.ToString(ViewState[SortDirection]);

            if (hdnsortingascdesc.Value == "ASC")
            {
                hdnsortingascdesc.Value = "DESC";
            }
            else if (hdnsortingascdesc.Value == "DESC")
            {
                hdnsortingascdesc.Value = "ASC";
            }
        }
        protected void grd_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridView grid1 = (GridView)sender;
            grid1.PageIndex = e.NewPageIndex;
            DataTable dt = (DataTable)ViewState[grid1.ClientID + "tblData"];
            string SortExpression = grid1.ClientID + "SortExpression";
            if (ViewState[SortExpression] == null)
            {
                dt = common.sortDatatable(dt, grid1, 2);
            }
            grid1.DataSource = dt;
            grid1.DataBind();
        }
        protected void grd_DataBound(object sender, EventArgs e)
        {
            GridView grid1 = (GridView)sender;
            fullGridPagerObj = (FullGridPager)fullGridPager[grid1.ClientID];
            if (fullGridPagerObj == null)
            {
                fullGridPagerObj = new FullGridPager(grid1, MaxVisible, "Page", "of");
            }
            fullGridPagerObj.CreateCustomPager(grid1.BottomPagerRow);
            fullGridPagerObj.PageGroups(grid1.BottomPagerRow);
            fullGridPager[grid1.ClientID] = fullGridPagerObj;
        }

        #endregion ProjectListing


        #region Report
        protected void GenerateReport(object sender, EventArgs e)
        {
            Random rn = new Random();
            hdnrandowmnumber.Value = rn.Next().ToString();
            string pdfpath = HostingEnvironment.MapPath("~/pdfs/" + hdnrandowmnumber.Value + ".pdf");

            GridView gvdetails = new GridView();
            DataTable dt2 = new DataTable();
            List<Projects.ClsProjectInformationPrp> data = ObjProjectInformation.GetAgencyProjects(txtSearch.Text.Trim(),"");
            DataTable dt = ListToDataTable.ToDataTable(data);
            if (dt.Rows.Count > 0)
            {
                if (hdnsortingascdesc.Value != "")
                {
                    GridView grid1 = (GridView)grdProjectListing;// (GridView)sender;
                    //grid1.DataSource = dt;
                    string sortingDirection = string.Empty;
                    string SortDirection = grid1.ClientID + "SortDirection";//"ContentPlaceHolder1_grdcompanyListing"
                    string SortExpression = grid1.ClientID + "SortExpression";
                    //string imgDir = common.getSorImage(ViewState[SortDirection]);
                    dt = CollageProjects.common.getSortedDataTable((dt), (GridView)grdProjectListing, hdnsortingascdesc.Value, ViewState[SortExpression], hdnSortExpression.Value);
                }
                else
                {
                    DataView dv = new DataView(dt);
                    dv.Sort = "ProjectName ASC";
                    dt = dv.ToTable();
                }

                dt2.Columns.Add("Project Number", typeof(string));
                dt2.Columns.Add("Project Name", typeof(string));
                dt2.Columns.Add("Address", typeof(string));
                dt2.Columns.Add("Client", typeof(string));
                dt2.Columns.Add("Category", typeof(string));
                dt2.Columns.Add("Status", typeof(string));
                dt2.Columns.Add("Start Date", typeof(string));
                dt2.Columns.Add("End Date", typeof(string));

                foreach (DataRow row in dt.Rows)
                {
                    dt2.Rows.Add(row["ProjectNumber"].ToString(), row["ProjectName"].ToString(), row["Address"].ToString() + ", " + row["CityName"].ToString() + ", " + row["StateShortName"].ToString(), row["CompanyName"].ToString(), row["ProjectCategoryName"].ToString(), row["Status"].ToString(), row["StartDate"].ToString(), row["EndDate"].ToString());
                }

                objgeneratReportsListing.Genratereportbind(dt2, "Agency", "Agency", pdfpath, "Agency", "", txtSearch.Text.Trim());

            }
        }
        protected void CSVGenerateReport(object sender, EventArgs e)
        {
            GridView gvdetails1 = new GridView();

            Random rn = new Random();
            hdnrandowmnumber.Value = rn.Next().ToString();
            string pdfpath = HostingEnvironment.MapPath("~/pdfs/" + hdnrandowmnumber.Value + ".pdf");
            GridView gvdetails = new GridView();
            DataTable dt2 = new DataTable();
            List<Projects.ClsProjectInformationPrp> data = ObjProjectInformation.GetAgencyProjects(txtSearch.Text.Trim(),"");
            DataTable dt = ListToDataTable.ToDataTable(data);
            if (dt.Rows.Count > 0)
            {
                if (hdnsortingascdesc.Value != "")
                {
                    GridView grid1 = (GridView)grdProjectListing;// (GridView)sender;
                    //grid1.DataSource = dt;
                    string sortingDirection = string.Empty;
                    string SortDirection = grid1.ClientID + "SortDirection";//"ContentPlaceHolder1_grdcompanyListing"
                    string SortExpression = grid1.ClientID + "SortExpression";
                    //string imgDir = common.getSorImage(ViewState[SortDirection]);
                    dt = CollageProjects.common.getSortedDataTable((dt), (GridView)grdProjectListing, hdnsortingascdesc.Value, ViewState[SortExpression], hdnSortExpression.Value);
                }
                else
                {
                    DataView dv = new DataView(dt);
                    dv.Sort = "ProjectName ASC";
                    dt = dv.ToTable();
                }

                dt2.Columns.Add("Project Number", typeof(string));
                dt2.Columns.Add("Project Name", typeof(string));
                dt2.Columns.Add("Address", typeof(string));
                dt2.Columns.Add("Client", typeof(string));
                dt2.Columns.Add("Category", typeof(string));
                dt2.Columns.Add("Status", typeof(string));
                dt2.Columns.Add("Start Date", typeof(string));
                dt2.Columns.Add("End Date", typeof(string));

                foreach (DataRow row in dt.Rows)
                {
                    dt2.Rows.Add(row["ProjectNumber"].ToString(), row["ProjectName"].ToString(), row["Address"].ToString() + ", " + row["CityName"].ToString() + ", " + row["StateShortName"].ToString(), row["CompanyName"].ToString(), row["ProjectCategoryName"].ToString(), row["Status"].ToString(), row["StartDate"].ToString(), row["EndDate"].ToString());
                }

                objgeneratReportsListing.GenratereportbindExcel(dt2, "Agency", "");
            }
        }
       protected void EmailAttachedReport(object sender, EventArgs e)
        {
            Random rn = new Random();
            hdnrandowmnumber1.Value = "Agency";// rn.Next().ToString();
          
            GridView gvdetails = new GridView();
            DataTable dt2 = new DataTable();
            List<Projects.ClsProjectInformationPrp> data = ObjProjectInformation.GetAgencyProjects(txtSearch.Text.Trim(),"");
            DataTable dt = ListToDataTable.ToDataTable(data);
            if (dt.Rows.Count > 0)
            {
                if (hdnsortingascdesc.Value != "")
                {
                    GridView grid1 = (GridView)grdProjectListing;// (GridView)sender;
                    //grid1.DataSource = dt;
                    string sortingDirection = string.Empty;
                    string SortDirection = grid1.ClientID + "SortDirection";//"ContentPlaceHolder1_grdcompanyListing"
                    string SortExpression = grid1.ClientID + "SortExpression";
                    //string imgDir = common.getSorImage(ViewState[SortDirection]);
                    dt = CollageProjects.common.getSortedDataTable((dt), (GridView)grdProjectListing, hdnsortingascdesc.Value, ViewState[SortExpression], hdnSortExpression.Value);
                }
                else
                {
                    DataView dv = new DataView(dt);
                    dv.Sort = "ProjectName ASC";
                    dt = dv.ToTable();
                }

                dt2.Columns.Add("Project Number", typeof(string));
                dt2.Columns.Add("Project Name", typeof(string));
                dt2.Columns.Add("Address", typeof(string));
                dt2.Columns.Add("Client", typeof(string));
                dt2.Columns.Add("Category", typeof(string));
                dt2.Columns.Add("Status", typeof(string));
                dt2.Columns.Add("Start Date", typeof(string));
                dt2.Columns.Add("End Date", typeof(string));

                foreach (DataRow row in dt.Rows)
                {
                    dt2.Rows.Add(row["ProjectNumber"].ToString(), row["ProjectName"].ToString(), row["Address"].ToString() + ", " + row["CityName"].ToString() + ", " + row["StateShortName"].ToString(), row["CompanyName"].ToString(), row["ProjectCategoryName"].ToString(), row["Status"].ToString(), row["StartDate"].ToString(), row["EndDate"].ToString());
                }
                objgeneratReportsListing.GenratereportEmailAttachment(dt2, "Agency", "Agency", "Agency", "Agency", "", txtSearch.Text, this);
            }
        }
        #endregion
    }
}