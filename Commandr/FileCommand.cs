using System;

namespace Commandr
{
    public class FileCommand
    {
        public FileCommand(String command, FileCommandType fileCommandType)
            : this(fileCommandType)
        {
            this.Command = command;
        }

        public FileCommand(Action command, FileCommandType fileCommandType)
            : this(fileCommandType)
        {
            this.ActionCommand = command;
        }

        private FileCommand(FileCommandType fileCommandType)
        {
            this.CommandType = fileCommandType;
        }

        public FileCommandType CommandType { get; set; }

        public String Command { get; private set; }

        public Action ActionCommand { get; private set; }
    }
}