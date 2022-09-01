using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Framework.DataBase;
using Framework.Entities;
using Framework.UI;

namespace MailSpy
{
    public class MailSpyWindowVM : ViewModelBase
    {
        #region Members

        private Command searchCommand;

        private IEnumerable<String> dataBaseNames;

        private IEnumerable<Entity> mailStates;

        #endregion

        #region Properties

        public Command SearchCommand
        {
            get
            {
                if (this.searchCommand == null)
                    this.searchCommand = new Command(() => this.Search());

                return this.searchCommand;
            }
        }

        public IEnumerable<String> DataBaseNames
        {
            get
            {
                if (this.dataBaseNames == null)
                    this.dataBaseNames = this.LoadDataBases();

                return this.dataBaseNames;
            }
        }

        public IEnumerable<Entity> MailStates
        {
            get
            {
                if (this.mailStates == null)
                    this.mailStates = new[] { new Entity(-1, "Any") }.Concat(this.LoadMailStates());

                return this.mailStates;
            }
        }

        public MailView SelectedItem { get; private set; }

        public Entity SelectedMailState { get; set; }

        public DateTime? DateFrom { get; set; }

        public DateTime? DateTo { get; set; }
        public String SubjectContains { get; set; }

        public String RecipientContains { get; set; }

        public String DataBase { get; set; }

        public IEnumerable<MailView> MailList { get; private set; }

        #endregion

        #region Methods

        public override void Initialize()
        {
            this.DataBase = this.DataBaseNames.First();

            this.DateFrom = DateTime.Now.Date.AddMonths(-1);
            this.DateTo = DateTime.Now.Date;

            this.SelectedMailState = this.MailStates.First();

            this.RaisePropertyChangedEvent("DateFrom");
            this.RaisePropertyChangedEvent("DateTo");
            this.RaisePropertyChangedEvent("DataBase");
        }

        public void Search()
        {
            try
            {
                var script = File.ReadAllText(".\\Scripts\\ReadMails.sql");

                var reader = new DataReader(Configuration.Default.ConnectionString);

                var parameterDateFrom = new KeyValuePair<String, Object>("dateFrom", this.DateFrom);
                var parameterDateTo = new KeyValuePair<String, Object>("dateTo", this.DateTo);
                var parameterSubjectContains = new KeyValuePair<String, Object>("subjectContains", String.IsNullOrWhiteSpace(this.SubjectContains) ? null : "%" + this.SubjectContains + "%");
                var parameterRecipientContains = new KeyValuePair<String, Object>("recipientContains", String.IsNullOrWhiteSpace(this.RecipientContains) ? null : "%" + this.RecipientContains + "%");
                var parameterMailState = new KeyValuePair<String, Object>("mailState", this.SelectedMailState == null ? null : (Int64?)this.SelectedMailState.Id);

                script = String.Format(script, this.DataBase);

                this.MailList = reader.Read<MailView>(script, parameterDateFrom, parameterDateTo, parameterSubjectContains, parameterRecipientContains, parameterMailState);

                this.RaisePropertyChangedEvent("MailList");
            }
            catch (Exception ex)
            {
                Framework.Utils.HandleException(ex);
            }
        }

        public void SelectItem(IList selected)
        {
            var selectedMails = selected.Cast<MailView>();

            if (selected != null)
                this.SelectedItem = selectedMails.FirstOrDefault();
            else
                selected = null;
        }

        public void ShowSelected()
        {
            if (this.SelectedItem != null)
            {
                var show = new ShowBody(this.DataBase, this.SelectedItem);

                show.Show();
            }
        }

        private IEnumerable<String> LoadDataBases()
        {
            var reader = new DataReader(Configuration.Default.ConnectionString);

            var script = File.ReadAllText(".\\Scripts\\ReadDbNames.sql");

            var values = reader.Read<ValueOf<String>>(script);

            return values.Select(v => v.Value).ToArray();
        }

        private IEnumerable<Entity> LoadMailStates()
        {
            var reader = new DataReader(Configuration.Default.ConnectionString);

            var script = String.Format(File.ReadAllText(".\\Scripts\\LoadMailStates.sql"), "Dev_FyoTrade");

            return reader.Read<Entity>(script);
        }

        #endregion
    }
}