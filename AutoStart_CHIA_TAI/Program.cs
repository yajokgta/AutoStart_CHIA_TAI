using System;
using System.IO;
using System.Linq;

namespace AutoStart_CHIA_TAI
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var dbContext = new CHIATAIDataContext(Extension._ConnectionString);
            var mstTemplate = dbContext.MSTTemplates.FirstOrDefault(x => x.DocumentCode == Extension._DocumentCode && x.IsActive == true);
            var files = Directory.GetFiles(Extension._DownloadPath); 
            if (files.Any())
            {
                foreach (var file in files)
                {
                    try
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var modules = fileName.Split('_').ToList();

                        var positionWorkCode = modules?[0];

                        modules.RemoveAt(0);

                        modules = modules.Select(s => s.Trim()).ToList();

                        var positionWorkName = string.Join(" ", modules);

                        var requester = dbContext.ViewEmployees.FirstOrDefault(x => x.EmployeeCode == Extension._ReqEmpCode);
                        var CurrentCom = dbContext.MSTCompanies.FirstOrDefault(x => x.CompanyCode == requester.CompanyCode);
                        string _CC = "";

                        TRNMemo objMemo = new TRNMemo();
                        objMemo.StatusName = Extension.Status._Completed;

                        objMemo.CreatedDate = DateTime.Now;
                        objMemo.CreatedBy = requester.NameEn;
                        objMemo.CreatorId = requester.EmployeeId;
                        objMemo.RequesterId = requester.EmployeeId;
                        objMemo.CNameTh = requester.NameTh;
                        objMemo.CNameEn = requester.NameEn;
                        objMemo.CurrentApprovalLevel = 0;
                        objMemo.CPositionId = requester.PositionId;
                        objMemo.CPositionTh = requester.PositionNameTh;
                        objMemo.CPositionEn = requester.PositionNameEn;
                        objMemo.CDepartmentId = requester.DepartmentId;
                        objMemo.CDepartmentTh = requester.DepartmentNameTh;
                        objMemo.CDepartmentEn = requester.DepartmentNameEn;
                        objMemo.RNameTh = requester.NameTh;
                        objMemo.RNameEn = requester.NameEn;
                        objMemo.RPositionId = requester.PositionId;
                        objMemo.RPositionTh = requester.PositionNameTh;
                        objMemo.RPositionEn = requester.PositionNameEn;
                        objMemo.RDepartmentId = requester.DepartmentId;
                        objMemo.RDepartmentTh = requester.DepartmentNameTh;
                        objMemo.RDepartmentEn = requester.DepartmentNameEn;

                        objMemo.ModifiedDate = DateTime.Now;
                        objMemo.ModifiedBy = objMemo.ModifiedBy;
                        objMemo.TemplateId = mstTemplate.TemplateId;
                        objMemo.TemplateName = mstTemplate.TemplateName;
                        objMemo.GroupTemplateName = mstTemplate.GroupTemplateName;
                        objMemo.RequestDate = DateTime.Now;
                        objMemo.CompanyId = 1;
                        objMemo.CompanyName = CurrentCom?.NameTh;
                        objMemo.MAdvancveForm = mstTemplate.AdvanceForm;
                        objMemo.TAdvanceForm = mstTemplate.AdvanceForm;

                        objMemo.TemplateSubject = mstTemplate.TemplateSubject;

                        objMemo.TemplateDetail = Guid.NewGuid().ToString().Replace("-", "");

                        objMemo.ProjectID = 0;
                        objMemo.DocumentCode = Services.genControlRunning(requester, mstTemplate.DocumentCode, objMemo, dbContext);
                        objMemo.DocumentNo = objMemo.DocumentCode;

                        //ADD CC
                        objMemo.CcPerson = _CC;

                        dbContext.TRNMemos.InsertOnSubmit(objMemo);
                        dbContext.SubmitChanges();

                        objMemo.PersonWaitingId = requester.EmployeeId;
                        objMemo.PersonWaiting = requester.NameTh;

                        var fileUploadPath = Services.moveFile(objMemo.TemplateDetail, file);

                        var businessName = mstTemplate.TemplateSubject.Split('-')?.Last();

                        var oldMemo = (from memo in dbContext.TRNMemos
                                       let memForm = (from f in dbContext.TRNMemoForms
                                                      where f.MemoId == memo.MemoId && f.obj_label == "รหัสตำแหน่งงาน" && f.obj_value == positionWorkCode select f)
                                       where memForm.Any()
                                       select memo)
                                       .OrderByDescending(o => o.RequestDate)
                                       .FirstOrDefault();

                        var mAdvanceForm = AdvanceFormExt.ReplaceDataProcess(mstTemplate.AdvanceForm, positionWorkCode, "รหัสตำแหน่งงาน");
                        mAdvanceForm = AdvanceFormExt.ReplaceDataProcess(mAdvanceForm, positionWorkName, "ชื่อตำแหน่งงาน");
                        mAdvanceForm = AdvanceFormExt.ReplaceDataProcess(mAdvanceForm, businessName, "BU");
                        mAdvanceForm = AdvanceFormExt.ReplaceDataProcess(mAdvanceForm, fileUploadPath, "รายละเอียดตำแหน่งงาน");

                        objMemo.MAdvancveForm = mAdvanceForm;
                        objMemo.MemoSubject = $"{mstTemplate.TemplateSubject}-{positionWorkCode}";
                        Console.WriteLine($"Create MemoId : {objMemo.MemoId}");

                        dbContext.SubmitChanges();
                        Extension.WriteLogFile($"MEMO CREATE ID : {objMemo.MemoId}");
                        Services.InsertTRNForm(objMemo.MemoId, Extension._ConnectionString);
                    }
                    catch (Exception ex)
                    {
                        Extension.WriteLogFile($"Error : {ex}");
                        Extension.WriteLogFile($"Error File : {file}");
                    }
                }
            }
        }
    }
}
