/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/
using System;

namespace ScriptEngine.Compiler
{
    class CodeBatchHierarchy
    {
        public CodeBatchHierarchy(CodeBatchHierarchy parent = null, int lineNumber = 0, int codePosition = -1)
        {
            Parent = parent;
            LineNumber = lineNumber;
            CodePosition = codePosition;
        }

        public CodeBatchHierarchy Parent { get; }
        public int LineNumber { get; }
        public int CodePosition { get; }

        public bool IsReachableFrom(CodeBatchHierarchy codeBlock)
        {
            var block = codeBlock;
            while (block != null)
            {
                if (object.ReferenceEquals(block, this))
                {
                    return true;
                }
                block = block.Parent;
            }
            return false;
        }
    }
}
