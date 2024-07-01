using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using AutoStart_CHIA_TAI;

namespace AutoStart_CHIA_TAI
{
    public class Services
    {

        public static string CleanString(string plainText)
        {
            return plainText.Replace("\r\n", "").Replace(" ", "").ToUpper().Trim();
        }

        public static string GenControlRunning(CHIATAIDataContext db)
        {
            var prefix = $"EDC-EA-{DateTime.Now.ToString("yyyy", new CultureInfo("en-GB"))}-{DateTime.Now.ToString("MM", new CultureInfo("en-GB"))}-";
            var listRunning = db.TRNControlRunnings.Where(x => x.Prefix.Contains(prefix)).ToList();
            string newRunning = "";
            string newCodeRunning = "";

            if (listRunning.Any())
            {
                var lastRunning = listRunning.LastOrDefault();
                newRunning = (lastRunning.Running + 1).ToString().PadLeft(3, '0');
                newCodeRunning = $"{prefix}{newRunning}";

                var trnRunning = new TRNControlRunning()
                {
                    TemplateId = 13,
                    Prefix = prefix,
                    Digit = 3,
                    Running = lastRunning.Running + 1,
                    RunningNumber = newCodeRunning,
                    CreateBy = "1",
                    CreateDate = DateTime.Now,
                };

                db.TRNControlRunnings.InsertOnSubmit(trnRunning);
            }

            else
            {
                newRunning = (1).ToString().PadLeft(3, '0');
                newCodeRunning = $"{prefix}{newRunning}";

                var trnRunning = new TRNControlRunning()
                {
                    TemplateId = 13,
                    Prefix = prefix,
                    Digit = 3,
                    Running = 1,
                    RunningNumber = newCodeRunning,
                    CreateBy = "1",
                    CreateDate = DateTime.Now,
                };

                db.TRNControlRunnings.InsertOnSubmit(trnRunning);
            }

            db.SubmitChanges();
            return newCodeRunning;
        }

        public static bool InsertTRNForm(int memoid, string con)
        {
            bool result = false;
            using (SqlConnection sqlCon = new SqlConnection(con))
            {
                sqlCon.Open();
                SqlCommand sql_cmnd = new SqlCommand("SP_InsertTRNFormByMemoID", sqlCon);
                sql_cmnd.CommandType = CommandType.StoredProcedure;
                sql_cmnd.Parameters.AddWithValue("@MemoID", SqlDbType.Int).Value = memoid;
                using (SqlDataReader oReader = sql_cmnd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        result = (bool)oReader["result"];
                    }
                }
                sqlCon.Close();
            }
            return result;
        }

        public static string moveFile(string guID, string _SourcePath)
        {
            var Attach = new TRNAttachFile();
            var fileName = Path.GetFileName(_SourcePath);

            var pathInAdvance = "";

            if (_SourcePath != string.Empty)
            {
                var path = Path.Combine(Extension._TempAttachmentPath, Extension._Temp, guID);
                var fullPath = Path.Combine(path, fileName);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                System.IO.File.Move(_SourcePath, fullPath);
                pathInAdvance = $"{fileName}|/{Extension._Temp}/{guID}/{fileName}";
            }
            return pathInAdvance;
        }

        public static string genControlRunning(ViewEmployee Emp, string DocumentCode, TRNMemo objTRNMemo, CHIATAIDataContext db)
        {
            string TempCode = DocumentCode;
            String sPrefixDocNo = $"{TempCode}-{DateTime.Now.Year.ToString()}-";
            int iRunning = 1;
            List<TRNMemo> temp = db.TRNMemos.Where(a => a.DocumentNo.ToUpper().Contains(sPrefixDocNo.ToUpper())).ToList();
            if (temp.Count > 0)
            {
                String sLastDocumentNo = temp.OrderBy(a => a.DocumentNo).Last().DocumentNo;
                if (!String.IsNullOrEmpty(sLastDocumentNo))
                {
                    List<String> list_LastDocumentNo = sLastDocumentNo.Split('-').ToList();

                    if (list_LastDocumentNo.Count >= 3)
                    {
                        iRunning = checkDataIntIsNull(list_LastDocumentNo[list_LastDocumentNo.Count - 1]) + 1;
                    }
                }
            }
            String sDocumentNo = $"{sPrefixDocNo}{iRunning.ToString().PadLeft(6, '0')}";

            try
            {

                var mstMasterDataList = db.MSTMasterDatas.Where(a => a.MasterType == "DocNo").ToList();

                if (mstMasterDataList != null)
                    if (mstMasterDataList.Count() > 0)
                    {
                        var getCompany = db.MSTCompanies.Where(a => a.CompanyId == objTRNMemo.CompanyId);
                        var getDepartment = db.MSTDepartments.Where(a => a.DepartmentId == Emp.DepartmentId);
                        var getDivision = db.MSTDivisions.Where(a => a.DivisionId == Emp.DivisionId);

                        string CompanyCode = "";
                        string DepartmentCode = "";
                        string DivisionCode = "";
                        if (getCompany != null)
                            if (!string.IsNullOrWhiteSpace(getCompany.First().CompanyCode)) CompanyCode = getCompany.First().CompanyCode;
                        if (DepartmentCode != null)
                            if (!string.IsNullOrWhiteSpace(getDepartment.First().DepartmentCode)) DepartmentCode = getDepartment.First().DepartmentCode;
                        if (DivisionCode != null)
                        {
                            if (getDivision.Count() > 0)
                                if (!string.IsNullOrWhiteSpace(getDivision.First().DivisionCode)) DivisionCode = getDivision.First().DivisionCode;
                        }
                        foreach (var getMaster in mstMasterDataList)
                        {
                            if (!string.IsNullOrWhiteSpace(getMaster.Value2))
                            {
                                var Tid_array = getMaster.Value2.Split('|');
                                string FixDoc = getMaster.Value1;
                                if (Tid_array.Count() > 0)
                                {
                                    if (Tid_array.Contains(objTRNMemo.TemplateId.ToString()))
                                    {
                                        sDocumentNo = DocNoGenerate(FixDoc, TempCode, CompanyCode, DepartmentCode, DivisionCode, db);
                                    }
                                }
                            }
                            else
                            {
                                string FixDoc = getMaster.Value1;
                                sDocumentNo = DocNoGenerate(FixDoc, TempCode, CompanyCode, DepartmentCode, DivisionCode, db);
                            }
                        }

                    }




            }
            catch (Exception ex) { }



            return sDocumentNo;
        }

        public static int checkDataIntIsNull(object Input)
        {
            int Results = 0;
            if (Input != null)
                int.TryParse(Input.ToString().Replace(",", ""), out Results);

            return Results;
        }

        public static string DocNoGenerate(string FixDoc, string DocCode, string CCode, string DCode, string DSCode, CHIATAIDataContext db, string FixRunning = "")
        {
            string sDocumentNo = "";
            int iRunning;
            if (!string.IsNullOrWhiteSpace(FixDoc))
            {
                int IndexRunning = 99;
                if (!string.IsNullOrWhiteSpace(FixRunning))
                {
                    var spFixDoc = FixDoc.Split('-');
                    for (int i = 0; i < spFixDoc.Length; i++)
                    {
                        if (spFixDoc[i] == FixRunning)
                        {
                            IndexRunning = i;
                        }
                    }
                }

                string y4 = DateTime.Now.ToString("yyyy");
                string y2 = DateTime.Now.ToString("yy");
                string m2 = DateTime.Now.ToString("MM");
                string m4 = DateTime.Now.ToString("MMMM");
                string CompanyCode = CCode;
                string DepartmentCode = DCode;
                string DivisionCode = DSCode;
                string FixCode = FixDoc;
                FixCode = FixCode.Replace("[CompanyCode]", CompanyCode);
                FixCode = FixCode.Replace("[DepartmentCode]", DepartmentCode);
                FixCode = FixCode.Replace("[DocumentCode]", DocCode);
                FixCode = FixCode.Replace("[DivisionCode]", DivisionCode);

                FixCode = FixCode.Replace("[YYYY]", y4);
                FixCode = FixCode.Replace("[YY]", y2);
                FixCode = FixCode.Replace("[MMMM]", m4);
                FixCode = FixCode.Replace("[MM]", m2);
                List<TRNMemo> tempfixDoc = new List<TRNMemo>();
                sDocumentNo = FixCode;

                if (IndexRunning != -1 && IndexRunning != 99)
                {
                    string FindDocNo = FixCode.Split('-')[IndexRunning].ToUpper();
                    if (IndexRunning == 0)
                    {
                        tempfixDoc = db.TRNMemos.Where(a => a.DocumentNo.ToUpper().StartsWith(FindDocNo)).ToList();
                    }
                    else
                    {
                        tempfixDoc = db.TRNMemos.Where(a => a.DocumentNo.ToUpper().Contains(FindDocNo)).ToList();
                    }
                }

                else
                {
                    tempfixDoc = db.TRNMemos.Where(a => a.DocumentNo.ToUpper().StartsWith(sDocumentNo.ToUpper())).ToList();
                }

                if (tempfixDoc.Count > 0)
                {
                    String sLastDocumentNofix = tempfixDoc.OrderBy(a => a.MemoId).Last().DocumentNo;
                    if (!String.IsNullOrEmpty(sLastDocumentNofix))
                    {
                        List<String> list_LastDocumentNofix = sLastDocumentNofix.Split('-').ToList();

                        if (list_LastDocumentNofix.Count >= 3)
                        {
                            iRunning = checkDataIntIsNull(list_LastDocumentNofix[list_LastDocumentNofix.Count - 1]) + 1;
                            sDocumentNo = $"{sDocumentNo}-{iRunning.ToString().PadLeft(6, '0')}";
                        }
                    }
                }

                else
                {
                    sDocumentNo = $"{sDocumentNo}-{1.ToString().PadLeft(6, '0')}";

                }
            }
            return sDocumentNo;
        }

        public static string getValueAdvanceForm(string AdvanceForm, string label)
        {
            string setValue = "";
            JObject jsonAdvanceForm = JObject.Parse(AdvanceForm);
            if (jsonAdvanceForm.ContainsKey("items"))
            {
                JArray itemsArray = (JArray)jsonAdvanceForm["items"];
                foreach (JObject jItems in itemsArray)
                {
                    JArray jLayoutArray = (JArray)jItems["layout"];
                    foreach (JToken jLayout in jLayoutArray)
                    {
                        JObject jTemplate = (JObject)jLayout["template"];
                        var getLabel = (String)jTemplate["label"];
                        if (label == getLabel)
                        {
                            JObject jdata = (JObject)jLayout["data"];
                            if (jdata != null)
                            {
                                if (jdata["value"] != null) setValue = jdata["value"].ToString();
                            }
                            break;
                        }
                    }
                }
            }

            return setValue;
        }
    }
}
