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
    class CtrlInf
    {
        string m_des;
        Dictionary<string, string> m_promap=new Dictionary<string, string>();
        List<string> m_parent=new List<string>();
        int m_iCur = 0;
        public string getdes()
        {
            return m_des;
        }
        public void addpro(string desname,string des)
        {
            m_promap.Add(desname, des);
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
            foreach(var parentName in parentlist)
            {
                addparent(parentName);
            }
        }
        public CtrlInf()
        { }
        public Dictionary<string, string> getprolist()
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
            m_strCtrlProDbPath = m_strCtrlDbPath = m_strCtrlDbPath.Substring(0, m_strCtrlDbPath.LastIndexOf(@"\"));
            m_strCtrlDbPath += @"\data\ctrls.dat";
            m_strCtrlProDbPath += @"\data\ctrlspro.dat";
            if (loadControlMap())
            {
                if (loadProMap())
                {
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
        public void GetProInf(string key,string pro, out string inf, ITextSnapshot currentSnapshot, out ITrackingSpan applicableToSpan, SnapshotSpan querySpan)
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
                    tinf.getprolist().TryGetValue(pro, out inf);
                    return;
                }
            }
            inf = null;
            applicableToSpan = null;
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
                        if (key == null || key.Length == 0||!m_controlMap.ContainsKey(key))
                        {
                            return;
                        }
                        //添加属性
                        var prolist = m_controlMap[key].getprolist();
                        foreach (KeyValuePair<string, string> pair in prolist)
                        {                           
                            list.Add(new Completion(pair.Key, pair.Key, pair.Value, ico, "s"));
                        }
                        //添加父窗口的属性
                        var parentlist = m_controlMap[key].GetParent();
                        foreach (string parent in parentlist)
                        {
                            if (m_controlMap.ContainsKey(parent))
                            {
                                prolist = m_controlMap[key].getprolist();
                                foreach (KeyValuePair<string, string> pair in prolist)
                                {
                                    list.Add(new Completion(pair.Key, pair.Key, pair.Value, ico, "s"));
                                }
                            }
                        }
                    }
                    break;
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
                    if(m_controlMap.ContainsKey(xn.Name))
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
        //控件列表
        public Dictionary<string, CtrlInf> m_controlMap = null;
    }
}