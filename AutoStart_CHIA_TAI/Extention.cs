using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace AutoStart_CHIA_TAI
{
    public class Extension
    {
        public class Status
        {
            public const string _NewRequest = "New Request";
            public const string _WaitForRequestorReview = "Wait for Requestor Review";
            public const string _WaitForApprove = "Wait for Approve";
            public const string _WaitForComment = "Wait for Comment";
            public const string _Rework = "Rework";
            public const string _Draft = "Draft";
            public const string _Completed = "Completed";
            public const string _Rejected = "Rejected";
            public const string _Cancelled = "Cancelled";
            public const string _Pending = "Pending";
            public const string _RequestCancel = "Request Cancel";
        }

        public static string _ReqEmpCode = ConfigurationSettings.AppSettings["ReqEmpCode"];
        public const string _Temp = "TempAttachment";

        public static string _BaseAPI = ConfigurationSettings.AppSettings["APIUrl"];

        public static string _User = ConfigurationSettings.AppSettings["UserSP"];
        public static string _Pass = ConfigurationSettings.AppSettings["PasswordSP"];

        public static string _LogFile
        {
            get
            {
                var basePath = Path.Combine(Directory.GetCurrentDirectory(), "LogFile");
                return basePath;
            }
        }
        public static string _ConnectionString = ConfigurationSettings.AppSettings["ConnectionString"];
        public static string _DownloadPath = ConfigurationSettings.AppSettings["DownloadPath"];
        public static string _DocumentCode = ConfigurationSettings.AppSettings["DocumentCode"];
        public static string _ComplatedPath = ConfigurationSettings.AppSettings["ComplatedPath"];
        public static string _HideConsole = ConfigurationSettings.AppSettings["HideConsole"];
        public static string _TempAttachmentPath
        {
            get
            {
                using (var dbContext = new CHIATAIDataContext(_ConnectionString))
                {
                    var mstDataFilePath = dbContext.MSTMasterDatas.FirstOrDefault(x => x.MasterType.ToLower().Contains("filepath") && x.IsActive == true);
                    if (mstDataFilePath != null)
                    {
                        return mstDataFilePath.Value1;
                    }

                    else
                    {
                        return string.Empty;
                    }
                }
            }
        }

        public static void WriteLogFile(string iText)
        {
            //var _LogFile = "D:\\WOLF\\Approve\\AutoStart_TYM\\LogFile\\";
            if (!Directory.Exists(_LogFile))
            {
                Directory.CreateDirectory(_LogFile);
            }

            var LogFilePath = String.Format("{0}{1}_Log.txt", _LogFile, DateTime.Now.ToString("yyyyMMdd"));

            using (StreamWriter outfile = new StreamWriter(LogFilePath, true))
            {
                StringBuilder sbLog = new StringBuilder();

                var ListText = iText.Split('|').ToArray();

                foreach (var s in ListText)
                {
                    sbLog.AppendLine(s);
                }

                outfile.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), sbLog.ToString()));
            }
        }
    }
    class AdvanceFormExt
    {
        public class RootObject
        {
            public List<Item> Items { get; set; }
        }

        public class Item
        {
            public string Id { get; set; }
            public List<Layout> Layout { get; set; }
        }

        public class Layout
        {
            public Template Template { get; set; }
            public object Data { get; set; }
            public string Guid { get; set; }
            public bool IsShow { get; set; }
        }

        public class Template
        {
            public string Type { get; set; }
            public string Label { get; set; }
            public string Alter { get; set; }
            public Attribute Attribute { get; set; }
        }

        public class Attribute
        {
            public string Require { get; set; }
            public string Description { get; set; }
            public string Length { get; set; }
            public string Default { get; set; }
            public string Readonly { get; set; }
            public Date Date { get; set; }
            public Time Time { get; set; }
            public List<Column> column { get; set; }
        }

        public class Column
        {
            public string label { get; set; }
            public string alter { get; set; }
            public Control control { get; set; }
        }

        public class Control
        {
            public Template template { get; set; }
            public DataControl Data { get; set; }
        }

        public class Date
        {
            public string Use { get; set; }
            public string UseDate { get; set; }
            public string FullYear { get; set; }
            public string Symbol { get; set; }
        }

        public class Time
        {
            public string Use { get; set; }
            public string UseSecond { get; set; }
            public string Symbol { get; set; }
        }

        public class Data
        {
            public string value { get; set; }
            public List<List<Row>> row { get; set; }
        }
        public class DataControl
        {
            public Value value { get; set; }
        }
        public class Value
        {
            public List<Item> Items { get; set; }
        }
        public class Row
        {
            public string label { get; set; }
            public string value { get; set; }
        }

        public class AdvanceForm
        {
            public string type { get; set; }
            public string alter { get; set; }
            public string guid { get; set; }
            public string label { get; set; }
            public string value { get; set; }
            public List<List<AdvanceFormRow>> row { get; set; }
        }

        public class AdvanceFormRow
        {
            public string label { get; set; }
            public string value { get; set; }
        }

        public static List<AdvanceForm> ToList(string memo)
        {
            List<AdvanceForm> listadvance = new List<AdvanceForm>();

            if (!string.IsNullOrEmpty(memo))
            {
                var data = JsonConvert.DeserializeObject<RootObject>(memo);

                foreach (var item in data.Items)
                {
                    foreach (var component in item.Layout)
                    {
                        List<List<AdvanceFormRow>> row = new List<List<AdvanceFormRow>>();

                        string guid = component.Guid;
                        string label = component.Template.Label;
                        string type = component.Template.Type;
                        var value = "";

                        try
                        {
                            var dataValue = JsonConvert.DeserializeObject<Data>(component.Data.ToString());

                            var itemData = dataValue;

                            value = itemData.value;

                            if (itemData.row != null && itemData.row.Any())
                            {
                                if (component.Template.Attribute.column.Any())
                                {
                                    var column = component.Template.Attribute.column;
                                    foreach (var itemrow in itemData.row)
                                    {
                                        int index = 0;
                                        var listRow = new List<AdvanceFormRow>();
                                        foreach (var itemx in itemrow)
                                        {
                                            itemx.label = column[index].label;
                                            listRow.Add(new AdvanceFormRow
                                            {
                                                label = itemx.label,
                                                value = itemx.value,
                                            });
                                            index++;
                                        }

                                        row.Add(listRow);
                                    }
                                }
                            }

                            listadvance.Add(new AdvanceForm
                            {
                                label = label,
                                value = value,
                                row = row,
                                guid = guid,
                                type = type
                            });
                        }
                        catch
                        {
                            try
                            {
                                var dataValue = JsonConvert.DeserializeObject<List<Data>>(component.Data.ToString());

                                var listItemData = dataValue;

                                foreach (var itemData in listItemData)
                                {
                                    value = itemData.value;

                                    if (itemData.row != null && itemData.row.Any())
                                    {
                                        if (component.Template.Attribute.column.Any())
                                        {
                                            var column = component.Template.Attribute.column;
                                            foreach (var itemrow in itemData.row)
                                            {
                                                int index = 0;
                                                var listRow = new List<AdvanceFormRow>();
                                                foreach (var itemx in itemrow)
                                                {
                                                    itemx.label = column[index].label;
                                                    listRow.Add(new AdvanceFormRow
                                                    {
                                                        label = itemx.label,
                                                        value = itemx.value,
                                                    });
                                                    index++;
                                                }

                                                row.Add(listRow);
                                            }
                                        }
                                    }

                                    listadvance.Add(new AdvanceForm
                                    {
                                        label = label,
                                        value = value,
                                        row = row,
                                        guid = guid,
                                        type = type
                                    });
                                }
                            }

                            catch
                            {

                            }
                        }
                    }
                }
            }

            return listadvance;
        }

        public static AdvanceForm ProcessData(Data itemData, Layout component)
        {
            var value = itemData.value;
            var row = new List<List<AdvanceFormRow>>();
            string guid = component.Guid;
            string label = component.Template.Label;
            string type = component.Template.Type;

            if (itemData.row != null && itemData.row.Any())
            {
                if (component.Template.Attribute.column.Any())
                {
                    var column = component.Template.Attribute.column;
                    foreach (var itemrow in itemData.row)
                    {
                        int index = 0;
                        var listRow = new List<AdvanceFormRow>();
                        foreach (var itemx in itemrow)
                        {
                            itemx.label = column[index].label;
                            listRow.Add(new AdvanceFormRow
                            {
                                label = itemx.label,
                                value = itemx.value,
                            });
                            index++;
                        }

                        row.Add(listRow);
                    }
                }
            }

            var advance = new AdvanceForm
            {
                label = label,
                value = value,
                row = row,
                guid = guid,
                type = type
            };

            return advance;
        }

        public static string ReplaceDataProcess(string destAdvanceForm, string value, string label)
        {
            JObject jsonAdvanceForm = JObject.Parse(destAdvanceForm);
            JArray itemsArray = (JArray)jsonAdvanceForm["items"];

            foreach (JObject item in itemsArray)
            {
                JArray layoutArray = (JArray)item["layout"];
                UpdateLayoutArray(layoutArray, value, label);
            }

            return JsonConvert.SerializeObject(jsonAdvanceForm);
        }

        private static void UpdateLayoutArray(JArray layoutArray, string value, string label)
        {
            for (int i = 0; i < layoutArray.Count; i++)
            {
                JObject template = (JObject)layoutArray[i]["template"];
                if (template["label"].ToString().Contains(label))
                {
                    JObject data = (JObject)layoutArray[i]["data"];
                    if (data != null)
                    {
                        data["value"] = value;
                    }
                }
            }
        }
        /*public static string ReplaceDataProcess(string DestAdvanceForm, string Value, string label)
        {
            JObject jsonAdvanceForm = JObject.Parse(DestAdvanceForm);
            JArray itemsArray = (JArray)jsonAdvanceForm["items"];
            foreach (JObject jItems in itemsArray)
            {
                JArray jLayoutArray = (JArray)jItems["layout"];

                if (jLayoutArray.Count >= 1)
                {
                    JObject jTemplateL = (JObject)jLayoutArray[0]["template"];

                    if (label.Contains((String)jTemplateL["label"]))
                    {

                        JObject jData = (JObject)jLayoutArray[0]["data"];
                        if (jData != null)
                        {
                            jData["value"] = Value;
                        }
                    }

                    if (jLayoutArray.Count > 1)
                    {
                        JObject jTemplateR = (JObject)jLayoutArray[1]["template"];

                        if (label.Contains((String)jTemplateR["label"]))
                        {

                            JObject jData = (JObject)jLayoutArray[1]["data"];
                            if (jData != null)
                            {
                                jData["value"] = Value;
                            }
                        }
                    }
                }
            }
            return JsonConvert.SerializeObject(jsonAdvanceForm);
        }*/
    }
}
