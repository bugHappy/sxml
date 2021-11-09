using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Windows.Media;
using System.Xml;

//soui数据分类 
//控件类 所有接在"<"后的都认为是一个控件包括skin styles里的属性
//控件公共属性
//控件自有属性
//

namespace SXml
{
    struct valueInf
    {
        public string m_value;
        public string m_des;
        public valueInf(string val, string des)
        {
            m_value = val;
            m_des = des;
        }
    }
    class ProInf
    {
        public ProInf(string des)
        {
            m_des = des;
        }
        public string m_des;
        public List<valueInf> m_values = new List<valueInf>();
        public void add(string value, string valuedes)
        {
            m_values.Add(new valueInf(value, valuedes));
        }
        public bool GetProValue(string value)
        {
            foreach (valueInf parent in m_values)
            {
                if (parent.m_value == value)
                {
                    return true;
                }
            }
            return false;
        }
    }
    class CtrlInf
    {
        string m_des;
        Dictionary<string, ProInf> m_promap = new Dictionary<string, ProInf>();
        List<string> m_parent = new List<string>();
        
        public string getdes()
        {
            return m_des;
        }
        public void addpro(string desname, string des)
        {
            m_promap.Add(desname, new ProInf(des));
        }
        public ProInf getpro(string desname)
        {
            ProInf retPorInf;
            m_promap.TryGetValue(desname, out retPorInf);
            return retPorInf;
        }
        public void addparent(string parentName)
        {
            if (m_parent.Contains(parentName))
                return;
            m_parent.Add(parentName);
        }
        public CtrlInf(string des, string v)
        {
            m_des = des;
            var parentlist = v.Split(',');
            foreach (var parentName in parentlist)
            {
                addparent(parentName);
            }
        }
        public CtrlInf()
        { }
        public Dictionary<string, ProInf> getprolist()
        {
            return m_promap;
        }
        public List<string> GetParent()
        {
            return m_parent;
        }

    }
    class SouiData
    {
        private static readonly SouiData instance = new SouiData();

        private SouiData() { LoadSouiDB();/**/ }

        public static SouiData GetInstance()
        {
            return instance;
        }
        private string m_strCtrlDbPath = string.Empty;
        private string m_strCtrlProDbPath = string.Empty;
        private string m_strCtrlProValueDbPath = string.Empty;
        private bool isLoad = false;
        /// <summary>
        /// 加载SOUI数据库
        /// </summary>
        /// <param name="dbpath">数据库全路径</param>
        /// <returns>加载是否成功</returns>
        private bool LoadSouiDB()
        {
            if (isLoad)
            {
                return true;
            }
            m_strCtrlDbPath = Assembly.GetExecutingAssembly().Location;
            m_strCtrlProValueDbPath = m_strCtrlProDbPath = m_strCtrlDbPath = m_strCtrlDbPath.Substring(0, m_strCtrlDbPath.LastIndexOf(@"\"));
            m_strCtrlDbPath += @"\data\ctrls.dat";
            m_strCtrlProDbPath += @"\data\ctrlspro.dat";
            m_strCtrlProValueDbPath += @"\data\provalue.dat";
            if (loadControlMap())
            {
                if (loadProMap())
                {
                    //不管加载属性提示是否正常
                    loadProVauleMap();
                    return isLoad = true;
                }
            }
            return false;
        }
        public enum MAPTYPE
        {
            C, P//, S
        }
        public void GetKeyInf(string key, out string inf, ITextSnapshot currentSnapshot, out ITrackingSpan applicableToSpan, SnapshotSpan querySpan)
        {
            if (m_controlMap.ContainsKey(key))
            {
                applicableToSpan = currentSnapshot.CreateTrackingSpan
                        (
                        querySpan.Start.Add(0).Position, 9, SpanTrackingMode.EdgeInclusive
                        );
                CtrlInf tinf;
                m_controlMap.TryGetValue(key, out tinf);
                inf = tinf.getdes();
                return;
            }
            //             foreach (KeyValuePair<string, CtrlInf> pair in m_controlMap)
            //             {
            //                 if (pair.Value.getprolist().ContainsKey(key))
            //                 {
            //                     applicableToSpan = currentSnapshot.CreateTrackingSpan
            //                             (
            //                             querySpan.Start.Add(0).Position, 9, SpanTrackingMode.EdgeInclusive
            //                             );
            //                     pair.Value.getprolist().TryGetValue(key, out inf);
            //                     return;
            //                 }
            //             }
            inf = null;
            applicableToSpan = null;
        }
        public void GetProInf(string key, string pro, out string inf, ITextSnapshot currentSnapshot, out ITrackingSpan applicableToSpan, SnapshotSpan querySpan)
        {
            if (m_controlMap.ContainsKey(key))
            {
                CtrlInf tinf;
                m_controlMap.TryGetValue(key, out tinf);
                if (tinf.getprolist().ContainsKey(pro))
                {
                    applicableToSpan = currentSnapshot.CreateTrackingSpan
                            (
                            querySpan.Start.Add(0).Position, 9, SpanTrackingMode.EdgeInclusive
                            );
                    ProInf _inf;
                    tinf.getprolist().TryGetValue(pro, out _inf);
                    inf = _inf.m_des;
                    return;
                }
                else
                {//父窗口的属性

                    if (GetProInf(key, ref pro, out inf))
                    {
                        applicableToSpan = currentSnapshot.CreateTrackingSpan(querySpan.Start.Add(0).Position, 9, SpanTrackingMode.EdgeInclusive);
                        return;
                    }
                }
            }
            else if (GetProInf("window", ref pro, out inf))
            {
                applicableToSpan = currentSnapshot.CreateTrackingSpan(querySpan.Start.Add(0).Position, 9, SpanTrackingMode.EdgeInclusive);
                return;
            }
            inf = null;
            applicableToSpan = null;
        }
        private bool GetProInf(string ctrlName, ref string pro, out string inf)
        {
            CtrlInf tinf;
            m_controlMap.TryGetValue(ctrlName, out tinf);
            if (tinf.getprolist().ContainsKey(pro))
            {
                ProInf _inf;
                tinf.getprolist().TryGetValue(pro, out _inf);
                inf = _inf.m_des;
                return true;
            }
            var parentlist = m_controlMap[ctrlName].GetParent();
            foreach (string parent in parentlist)
            {
                if (m_controlMap.ContainsKey(parent))
                {
                    if (GetProInf(parent, ref pro, out inf))
                        return true;
                }
            }
            inf = null;
            return false;
        }
        public void GetMap(XML_TYPE xmltype, out List<Completion> list, MAPTYPE mapttype, IGlyphService gs, string key = null)
        {
            list = new List<Completion>();
            ImageSource ico = gs.GetGlyph(StandardGlyphGroup.GlyphKeyword, StandardGlyphItem.GlyphItemPublic);
            switch (mapttype)
            {
                case MAPTYPE.C:
                    {
                        foreach (KeyValuePair<string, CtrlInf> pair in m_controlMap)
                        {
                            //第一个是显示的单词，第二个是插入的单词，第三个说明
                            list.Add(new Completion(pair.Key, pair.Key, pair.Value.getdes(), ico, "c"));
                        }
                    }
                    break;
                case MAPTYPE.P:
                    {
                        addProToList(ref list, key, ref ico);
                    }
                    break;
            }
        }

        public void GetMap(XML_TYPE xmltype, out List<Completion> list, IGlyphService gs, string key, string pro)
        {
            list = new List<Completion>();
            ImageSource ico = gs.GetGlyph(StandardGlyphGroup.GlyphKeyword, StandardGlyphItem.GlyphItemPublic);
            AddProValue(ref list, key,pro, ref ico);
        }

        private void AddProValue(ref List<Completion> list, string ctrlName,string proName, ref ImageSource ico)
        {
            if (ctrlName == null || ctrlName.Length == 0 || !m_controlMap.ContainsKey(ctrlName))
            {
                var defprolist = m_controlMap["window"].getprolist();
                foreach (KeyValuePair<string, ProInf> pair in defprolist)
                {
                    if (pair.Key == proName)
                    {
                        foreach (var value in pair.Value.m_values)
                        {
                            list.Add(new Completion(value.m_value, value.m_value, value.m_des, ico, "s"));
                        }
                    }
                }
                return;
            }
            //添加属性
            var prolist = m_controlMap[ctrlName].getprolist();
            foreach (KeyValuePair<string, ProInf> pair in prolist)
            {
                if (pair.Key==proName)
                {
                    foreach (var value in pair.Value.m_values)
                    {
                        list.Add(new Completion(value.m_value, value.m_value, value.m_des, ico, "s"));
                    }
                    return;
                }
            }
            //添加父窗口的属性
            var parentlist = m_controlMap[ctrlName].GetParent();
            foreach (string parent in parentlist)
            {
                if (m_controlMap.ContainsKey(parent))
                {
                    AddProValue(ref list, parent,proName, ref ico);
                }
            }
        }


        private void addProToList(ref List<Completion> list, string ctrlName, ref ImageSource ico)
        {
            if (ctrlName == null || ctrlName.Length == 0 || !m_controlMap.ContainsKey(ctrlName))
            {
                var defprolist = m_controlMap["window"].getprolist();
                foreach (KeyValuePair<string, ProInf> pair in defprolist)
                {
                    list.Add(new Completion(pair.Key, pair.Key, pair.Value.m_des, ico, "s"));
                }
                return;
            }
            //添加属性
            var prolist = m_controlMap[ctrlName].getprolist();
            foreach (KeyValuePair<string, ProInf> pair in prolist)
            {
                list.Add(new Completion(pair.Key, pair.Key, pair.Value.m_des, ico, "s"));
            }
            //添加父窗口的属性
            var parentlist = m_controlMap[ctrlName].GetParent();
            foreach (string parent in parentlist)
            {
                if (m_controlMap.ContainsKey(parent))
                {
                    addProToList(ref list, parent, ref ico);
                }
            }
        }
        private bool loadControlMap()
        {
            try
            {
                m_controlMap = new Dictionary<string, CtrlInf>();
                XmlDocument xmlDoc;
                xmlDoc = new XmlDocument();
                xmlDoc.Load(m_strCtrlDbPath); //加载xml文件
                XmlNodeList nodeList = xmlDoc.SelectSingleNode("/root/ctrls").ChildNodes;
                foreach (XmlNode xn in nodeList)
                {
                    m_controlMap.Add(xn.Name, new CtrlInf(((XmlElement)xn).GetAttribute("des"), ((XmlElement)xn).GetAttribute("parent")));
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private bool loadProMap()
        {
            try
            {
                XmlDocument xmlDoc;
                xmlDoc = new XmlDocument();
                xmlDoc.Load(m_strCtrlProDbPath); //加载xml文件
                XmlNodeList nodeList = xmlDoc.SelectSingleNode("/root/ctrls_property").ChildNodes;
                foreach (XmlNode xn in nodeList)
                {
                    if (m_controlMap.ContainsKey(xn.Name))
                    {
                        XmlElement xe = (XmlElement)xn;
                        XmlNodeList nodeList2 = xe.ChildNodes;
                        foreach (XmlNode xn2 in nodeList2)
                        {
                            m_controlMap[xn.Name].addpro(xn2.Name, ((XmlElement)xn2).GetAttribute("des"));
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        private bool loadProVauleMap()
        {
            try
            {
                XmlDocument xmlDocValue;
                xmlDocValue = new XmlDocument();
                xmlDocValue.Load(m_strCtrlProValueDbPath); //加载xml文件
                XmlNodeList nodeList = xmlDocValue.SelectSingleNode("/root/property_value").ChildNodes;
                foreach (XmlNode xn in nodeList)
                {
                    if (m_controlMap.ContainsKey(xn.Name))
                    {
                        XmlElement xe = (XmlElement)xn;
                        XmlNodeList nodeList2 = xe.ChildNodes;
                        foreach (XmlNode xn2 in nodeList2)
                        {
                            ProInf inf = m_controlMap[xn.Name].getpro(xn2.Name);
                            if (inf != null)
                            {
                                XmlNode vauleNode = xn2.SelectSingleNode("value");
                                if (vauleNode != null)
                                {
                                    foreach (var vaule in vauleNode.Attributes)
                                    {
                                        inf.add(((XmlAttribute)vaule).Name, ((XmlAttribute)vaule).Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        //控件列表
        public Dictionary<string, CtrlInf> m_controlMap = null;
    }
}