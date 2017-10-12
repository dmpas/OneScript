/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using OneScript.DebugProtocol;

using ScriptEngine;
using ScriptEngine.Compiler;
using ScriptEngine.Environment;
using ScriptEngine.HostedScript;
using ScriptEngine.HostedScript.Library;
using ScriptEngine.Machine;

namespace oscript
{
    class ExecuteCompiledBehavior : AppBehavior, IHostApplication, ISystemLogWriter
    {
        string[] _scriptArgs;
        string _path;

        public ExecuteCompiledBehavior(string path, string[] args)
        {
            _scriptArgs = args;
            _path = path;
        }
        
        public IDebugController DebugController { get; set; }

        public override int Execute()
        {
            if (!System.IO.File.Exists(_path))
            {
                Echo($"Script file is not found '{_path}'");
                return 2;
            }

            SystemLogger.SetWriter(this);

            var engine = new HostedScriptEngine();
            engine.Initialize();
            
            ScriptModuleHandle module;
            
            using (var codeStream = new FileStream(_path, FileMode.Open))
            using (var binReader = new BinaryReader(codeStream))
            {
                var modulesCount = binReader.ReadInt32();

                var reader = new ModulePersistor();

                var entry = reader.Read(codeStream);
                --modulesCount;

                while (modulesCount-- > 0)
                {
                    var userScript = reader.Read(codeStream);
                    engine.LoadUserScript(userScript);
                }

                module = entry.Module;
            }
            
            var src = new BinaryCodeSource(module, _path);
            var process = engine.CreateProcess(this, module, src);

            return process.Start();
        }

        #region IHostApplication Members

        public void Echo(string text, MessageStatusEnum status = MessageStatusEnum.Ordinary)
        {
            ConsoleHostImpl.Echo(text, status);
        }

        public void ShowExceptionInfo(Exception exc)
        {
            ConsoleHostImpl.ShowExceptionInfo(exc);
        }

        public bool InputString(out string result, int maxLen)
        {
            return ConsoleHostImpl.InputString(out result, maxLen);
        }

        public string[] GetCommandLineArguments()
        {
            return _scriptArgs;
        }

        #endregion

        public void Write(string text)
        {
            Console.Error.WriteLine(text);
        }
        
        internal class BinaryCodeSource : ICodeSource
        {
            private ScriptModuleHandle _mh;

            public BinaryCodeSource(ScriptModuleHandle mh, string path)
            {
                _mh = mh;
                SourceDescription = path;
            }

            #region ICodeSource Members

            public string SourceDescription { get; }

            public string Code => "<Source is not available>";

            #endregion
        }

    }
}
