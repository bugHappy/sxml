using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
namespace SXml
{
    enum XML_TYPE
    {
        Unknow,Skin,Layout,String,Value
    }
    internal class SouiCompletionSource : ICompletionSource
    {
        private SouiCompletionSourceProvider m_sourceProvider;
        private ITextBuffer m_textBuffer;
        private List<Completion> m_compList;
        private XML_TYPE m_xmlType = XML_TYPE.Unknow;
        public SouiCompletionSource(SouiCompletionSourceProvider sourceProvider, ITextBuffer textBuffer)
        {
            m_sourceProvider = sourceProvider;
            m_textBuffer = textBuffer;
        }
        void ICompletionSource.AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            if (completionSets.Count != 0)
            {
                //                 completionSets.Clear();
                //                 session.Dismiss();
                //此处为XML激活的自动完成比如输入 <时
                return;
            }
            if(m_xmlType==XML_TYPE.Unknow)
            {

            }
            SnapshotPoint? currentPoint = session.GetTriggerPoint(m_textBuffer.CurrentSnapshot);// (session.TextView.Caret.Position.BufferPosition);
            if (!currentPoint.HasValue)
                return;
            ITextSnapshotLine TextSnapshotLine = session.TextView.TextSnapshot.GetLineFromPosition(currentPoint.Value);
            ITextStructureNavigator navigator = m_sourceProvider.NavigatorService.GetTextStructureNavigator(m_textBuffer);
            string xmlText = TextSnapshotLine.GetText();
            SnapshotPoint linestart = TextSnapshotLine.Start;
            xmlText = xmlText.Substring(0, currentPoint.Value - linestart.Position);
            //向前查找 <或>或者空格
            //string xmlText= m_textBuffer.CurrentSnapshot.GetText();
            int pos1 = xmlText.LastIndexOf("<");
            int pos2 = xmlText.LastIndexOf(" ");
            int pos3 = xmlText.LastIndexOf(">");
            int pos4 = xmlText.LastIndexOf("=");
           
            if (pos3 > pos1)
                return;//标签已关闭
            if (pos1 > pos2)
            {                
                SouiData.GetInstance().GetMap(m_xmlType,out m_compList, SouiData.MAPTYPE.C, m_sourceProvider.GlyphService);                
                var set = new CompletionSet(
                    "SOUI",
                    "SOUI",
                    FindTokenSpanAtPosition(session.GetTriggerPoint(m_textBuffer), session),
                    m_compList,
                    null);
//                 if(completionSets.Count>0)
//                 {
//                     completionSets.Clear();
//                 }
                completionSets.Add(set);
            }
            else if (pos4 > pos2)
            {                
                return;//属性值
            }
            else
            {               
                string key = "";
                if (pos1 > pos3)
                {
                    if (xmlText[pos1 + 1] != '/')//闭合节点
                    {
                        int pos = xmlText.IndexOf(" ", pos1);//<0123 56
                        pos1++;
                        key = xmlText.Substring(pos1, pos - pos1);
                    }
                }               
                if (key.Length != 0)
                {
                    SouiData.GetInstance().GetMap(m_xmlType ,out m_compList, SouiData.MAPTYPE.P, m_sourceProvider.GlyphService, key);
                    var set = new CompletionSet(
                        "SOUI",
                        "SOUI",
                        FindTokenSpanAtPosition(session.GetTriggerPoint(m_textBuffer), session),
                        m_compList,
                        null);
                    if (completionSets.Count > 0)
                    {
                        completionSets.Clear();
                    }
                    completionSets.Add(set);
                }
            }
        }
        private ITrackingSpan FindTokenSpanAtPosition(ITrackingPoint point, ICompletionSession session)
        {
            SnapshotPoint currentPoint = (session.TextView.Caret.Position.BufferPosition) - 1;
            ITextStructureNavigator navigator = m_sourceProvider.NavigatorService.GetTextStructureNavigator(m_textBuffer);
            TextExtent extent = navigator.GetExtentOfWord(currentPoint);
            return currentPoint.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
        }
        private bool m_isDisposed;
        public void Dispose()
        {
            if (!m_isDisposed)
            {
                GC.SuppressFinalize(this);
                m_isDisposed = true;
            }
        }

    }
}
