using System;

namespace TestRunner
{
    #region Enum

    public enum TestState
    {
        Failed,
        Passed,
        Inconclusive,
        Integration
    }

    #endregion

    public class TestEntity
    {
        #region Members

        public TestState State;
        public String Description;
        public String Location;

        #endregion

        #region Constructor

        public TestEntity(String info)
        {
            String[] parts = info.Split(new String[] {
                " ",
                ".Tests.Unit.",
                ".Tests."
            }, StringSplitOptions.RemoveEmptyEntries);

            #region Switch

            switch (parts[0])
            {
                case "Failed":
                    this.State = TestState.Failed;
                    break;

                case "Passed":
                    this.State = TestState.Passed;
                    break;

                default:
                    this.State = TestState.Inconclusive;
                    break;
            }

            #endregion

            if (info.Contains("Integration"))
                this.State = TestState.Integration;

            this.Location = parts[1];
            this.Description = parts[2].Replace("+", ":\n").Replace('_', ' ').Replace(".", ": ");
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return this.Description;
        }

        #endregion
    }
}