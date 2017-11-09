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
            SnapshotSpan pre = navigator.GetSpanOfPreviousSibling(extent.Span);
            string preText = pre.GetText();
            string searchText = extent.Span.GetText();
            string desText;
            if (preText == "<")//控件
            {
                SouiData.GetInstance().GetKeyInf(searchText, out desText, currentSnapshot, out applicableToSpan, querySpan);
                if (desText != null)
                    quickInfoContent.Add(desText);
                else
                {
                    applicableToSpan = null;
                    quickInfoContent.Add("");
                }
                return;
            }
            string TempText=preText;
            while (pre.Start >= 0)
            {
                if (preText == ">")
                    break;
                if(preText == "<")
                {
                    SouiData.GetInstance().GetProInf(TempText,searchText, out desText, currentSnapshot, out applicableToSpan, querySpan);
                    if (desText != null)
                    {
                        quickInfoContent.Add(desText);
                        return;
                    }
                    break;
                }
                TempText = preText;
                SnapshotPoint oldpos = pre.Start;
                pre = navigator.GetSpanOfPreviousSibling(pre);
                if (pre.Start == oldpos)
                    break;
                preText = pre.GetText();
            }
            applicableToSpan = null;
            quickInfoContent.Add("");
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
