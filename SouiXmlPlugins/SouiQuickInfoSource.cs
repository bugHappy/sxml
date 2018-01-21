using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace SXml
{  
    internal class SouiQuickInfoSource : IQuickInfoSource
    {
       

        private SouiQuickInfoSourceProvider m_provider;
        private ITextBuffer m_subjectBuffer;
        public SouiQuickInfoSource(SouiQuickInfoSourceProvider provider, ITextBuffer subjectBuffer)
        {
            m_provider = provider;
            m_subjectBuffer = subjectBuffer;
        }        
        void IQuickInfoSource.AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan)
        {
            // Map the trigger point down to our buffer.
            SnapshotPoint? subjectTriggerPoint = session.GetTriggerPoint(m_subjectBuffer.CurrentSnapshot);
            if (!subjectTriggerPoint.HasValue)
            {
                applicableToSpan = null;
                return;
            }
            ITextSnapshot currentSnapshot = subjectTriggerPoint.Value.Snapshot;
            SnapshotSpan querySpan = new SnapshotSpan(subjectTriggerPoint.Value, 0);
            //look for occurrences of our QuickInfo words in the span
            ITextStructureNavigator navigator = m_provider.NavigatorService.GetTextStructureNavigator(m_subjectBuffer); 
            TextExtent extent = navigator.GetExtentOfWord(subjectTriggerPoint.Value);            
            string searchText = extent.Span.GetText();
            if (searchText.Length < 2)//一般都是一个符号
            {
                applicableToSpan = null;
                return;
            }               
            SnapshotSpan ssPrevious = navigator.GetSpanOfPreviousSibling(extent.Span);
            SnapshotSpan ssEnclosing = navigator.GetSpanOfEnclosing(extent.Span);
            
            string strPreText = ssPrevious.GetText();
            string strEnclosing = ssEnclosing.GetText();            
            string desText,strCtrlName;
            if (strPreText == "<"|| strPreText == "</" || strEnclosing.StartsWith("</"))//控件名
            {
                SouiData.GetInstance().GetKeyInf(searchText, out desText, currentSnapshot, out applicableToSpan, querySpan);
                if (desText != null)
                    quickInfoContent.Add(desText);
                else
                {
                    applicableToSpan = null;
                }
                return;
            }
            else if (strPreText =="=\"")//属性值
            {
                applicableToSpan = null;
                return;
            }
            //属性名
           
            else if (strEnclosing.StartsWith("<"))
            {
                strCtrlName= navigator.GetExtentOfWord(ssEnclosing.Start+1).Span.GetText();

                if(strCtrlName==null||strCtrlName.Length==0)
                {
                    applicableToSpan = null;
                    return;
                }
                SouiData.GetInstance().GetProInf(strCtrlName, searchText, out desText, currentSnapshot, out applicableToSpan, querySpan);
                if (desText != null)
                {
                    quickInfoContent.Add(desText);
                    return;
                }
            }
            else if (strPreText.StartsWith("<"))
            {
                strCtrlName = navigator.GetExtentOfWord(ssPrevious.Start + 1).Span.GetText();

                if (strCtrlName == null || strCtrlName.Length == 0)
                {
                    applicableToSpan = null;
                    return;
                }
                SouiData.GetInstance().GetProInf(strCtrlName, searchText, out desText, currentSnapshot, out applicableToSpan, querySpan);
                if (desText != null)
                {
                    quickInfoContent.Add(desText);
                    return;
                }
            }
            strCtrlName =strPreText;
            while (ssPrevious.Start > 0)
            {
                if(strPreText == "<")
                {
                    SouiData.GetInstance().GetProInf(strCtrlName, searchText, out desText, currentSnapshot, out applicableToSpan, querySpan);
                    if (desText != null)
                    {
                        quickInfoContent.Add(desText);
                        return;
                    }
                    break;
                }
                strCtrlName = strPreText;
                ssPrevious = navigator.GetExtentOfWord(ssPrevious.Start-1).Span;
                strPreText = ssPrevious.GetText();
            }
            applicableToSpan = null;
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
