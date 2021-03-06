﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using HeavyDuck.Utilities;
using HeavyDuck.Utilities.Forms;

namespace HeavyDuck.Dnd.MacroMaker.Forms
{
    public partial class Main : Form
    {
        protected const string WEAPON_NONE = "NONE";
        protected const string WEAPON_UNARMED = "Unarmed";
        protected const string MACRO_ELEMENT_NAME = "net.rptools.maptool.model.MacroButtonProperties";

        private readonly CompendiumHelper m_compendium = new CompendiumHelper();
        private bool m_use_compendium;

        public Main()
        {
            InitializeComponent();

            // use form icon from resource
            this.Icon = Properties.Resources.computebot;

            // event handlers
            this.Load += new EventHandler(Main_Load);
            this.FormClosed += new FormClosedEventHandler(Main_FormClosed);
            browse_character_button.Click += new EventHandler(browse_character_button_Click);
            browse_macro_button.Click += new EventHandler(browse_macro_button_Click);
            go_button.Click += new EventHandler(go_button_Click);
        }

        #region Event Handlers

        private void Main_Load(object sender, EventArgs e)
        {
            // let the compendium helper load saved cookies
            m_compendium.LoadCookies();
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            // save cookies
            m_compendium.SaveCookies();
        }

        private void browse_character_button_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.CheckFileExists = true;
                d.Filter = "Character Builder Files (*.dnd4e)|*.dnd4e";

                if (d.ShowDialog(this) == DialogResult.OK)
                    character_box.Text = d.FileName;
            }
        }

        private void browse_macro_button_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog d = new SaveFileDialog())
            {
                d.AddExtension = true;
                d.Filter = "MapTool Macro Sets (*.mtmacset)|*.mtmacset";
                d.OverwritePrompt = true;
                d.ValidateNames = true;

                if (d.ShowDialog(this) == DialogResult.OK)
                    macro_box.Text = d.FileName;
            }
        }

        private void go_button_Click(object sender, EventArgs e)
        {
            ProgressDialog d = new ProgressDialog();
            string character_path = character_box.Text;
            string macro_path = macro_box.Text;
            List<PowerInfo> powers = new List<PowerInfo>();
            Dictionary<string, int> skills = new Dictionary<string, int>();
            System.Text.RegularExpressions.Regex img_remover = new System.Text.RegularExpressions.Regex(@"<img.*?>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // save the value of the checkbox
            m_use_compendium = compendium_check.Checked;

            d.AddTask((progress) =>
            {
                progress.Update("Scanning character sheet...");

                using (FileStream fs = File.OpenRead(character_path))
                {
                    XPathDocument doc = new XPathDocument(fs);
                    XPathNavigator nav = doc.CreateNavigator();
                    XPathNodeIterator power_iter, weapon_iter;

                    // select all power nodes
                    power_iter = nav.Select("/D20Character/CharacterSheet/PowerStats/Power");

                    // update the progress steps
                    progress.Update(0, power_iter.Count);

                    // iterate them
                    while (power_iter.MoveNext())
                    {
                        string name;
                        string action_type;
                        List<string> compendium_info = new List<string>();
                        PowerUsage usage;
                        XPathNavigator power_url_nav = null;

                        try
                        {
                            // read the basic stuff
                            name = power_iter.Current.SelectSingleNode("@name").Value;
                            usage = Util.EnumParse<PowerUsage>(power_iter.Current.SelectSingleNode("specific[@name = 'Power Usage']").Value.Replace("-", ""));
                            action_type = power_iter.Current.SelectSingleNode("specific[@name = 'Action Type']").Value.Trim();

                            // get the url for the power in the compendium
                            if (m_use_compendium)
                                power_url_nav = nav.SelectSingleNode("//RulesElementTally/RulesElement[@name = \"" + name + "\"]/@url");

                            // ...and if we did that, pull down the power description
                            if (power_url_nav != null)
                            {
                                HtmlAgilityPack.HtmlDocument scraper_doc = new HtmlAgilityPack.HtmlDocument();
                                HtmlAgilityPack.HtmlNodeNavigator scraper_result;
                                XPathNodeIterator content_iter;
                                XPathNavigator scraper_nav = null;

                                // slurp
                                try
                                {
                                    using (Stream s = m_compendium.GetEntryByUrl(power_url_nav.Value))
                                    {
                                        scraper_doc.Load(s, new UTF8Encoding());
                                        scraper_nav = scraper_doc.CreateNavigator();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // if the user clicked cancel, stop attempting compendium stuff
                                    if (ex is ApplicationException && ex.Message.ToLowerInvariant().Contains("user refused"))
                                        m_use_compendium = false;
                                    else
                                        MessageBox.Show(ex.ToString(), "Compendium Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }

                                // if the web request succeeded...
                                if (scraper_nav != null)
                                {
                                    // select all the detail p tags
                                    content_iter = scraper_nav.Select("//div[@id = 'detail']/p");

                                    // loop through them
                                    while (content_iter.MoveNext())
                                    {
                                        string line;

                                        // cast to HtmlNodeNavigator
                                        scraper_result = content_iter.Current as HtmlAgilityPack.HtmlNodeNavigator;

                                        // skip publishing stuff
                                        if (scraper_result == null || scraper_result.CurrentNode.InnerHtml.ToLowerInvariant().Contains("published in"))
                                            continue;

                                        // grab the line, strip images, and remove [] stuff
                                        line = scraper_result.CurrentNode.InnerHtml;
                                        line = img_remover.Replace(line, "-");
                                        line = line.Replace("[W]", "<i>W</i>");

                                        // add the line
                                        compendium_info.Add(line);
                                    }
                                }
                            }

                            // select weapons
                            weapon_iter = power_iter.Current.Select("Weapon");

                            // some powers aren't attacks or don't use weapons
                            if (weapon_iter.Count < 1)
                            {
                                powers.Add(new PowerInfo(name, compendium_info, WEAPON_NONE, usage, action_type));
                            }
                            else
                            {
                                while (weapon_iter.MoveNext())
                                {
                                    PowerInfo power = new PowerInfo(name, compendium_info, weapon_iter.Current.SelectSingleNode("@name").Value, usage, action_type);

                                    // when there is more than one weapon, skip the Unarmed weapon
                                    if (power.Weapon == WEAPON_UNARMED && weapon_iter.Count > 1)
                                        continue;

                                    // read the attack info
                                    power.AttackBonus = weapon_iter.Current.SelectSingleNode("AttackBonus").ValueAsInt;
                                    power.AttackStat = weapon_iter.Current.SelectSingleNode("AttackStat").Value.Trim();
                                    power.DamageExpression = weapon_iter.Current.SelectSingleNode("Damage").Value.Trim();
                                    power.Defense = weapon_iter.Current.SelectSingleNode("Defense").Value.Trim();

                                    // all powers list an Unarmed weapon, even if they don't include an attack, set these to WEAPON_NONE
                                    if (power.Defense == "")
                                        power.Weapon = WEAPON_NONE;

                                    // add it to the list
                                    powers.Add(power);
                                }
                            }


                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }

                        // wait briefly to avoid spamming compendium
                        if (m_use_compendium)
                            System.Threading.Thread.Sleep(200);

                        // advance progress
                        progress.Advance();
                    }

                    // detect skills from the RulesElementTally
                    power_iter = nav.Select("//RulesElementTally/RulesElement[@type = 'Skill']/@name");

                    // read the value for each one
                    while (power_iter.MoveNext())
                    {
                        string name = power_iter.Current.Value;
                        int value;
                        XPathNavigator value_node = nav.SelectSingleNode("/D20Character/CharacterSheet/StatBlock/Stat[@name = '" + name + "']/@value");

                        if (value_node != null && int.TryParse(value_node.Value, out value))
                            skills[name] = value;
                    }
                }
            });

            d.AddTask((progress) =>
            {
                progress.Update("Writing macros...");

                using (FileStream fs = File.Open(macro_path, FileMode.Create, FileAccess.Write))
                {
                    XmlWriterSettings settings;

                    settings = new XmlWriterSettings();
                    settings.Encoding = new System.Text.ASCIIEncoding();
                    settings.Indent = true;
                    settings.IndentChars = "  ";
                    settings.OmitXmlDeclaration = false;

                    using (XmlWriter w = XmlWriter.Create(fs, settings))
                    {
                        w.WriteStartDocument();
                        w.WriteStartElement("list");

                        foreach (PowerInfo power in powers)
                        {
                            StringBuilder command = new StringBuilder();
                            string text, background;

                            // skip basic attacks with no weapons
                            if (IsPowerBasicAttack(power.Name) && power.Weapon == WEAPON_NONE)
                                continue;

                            // begin macro definition
                            w.WriteStartElement(MACRO_ELEMENT_NAME);
                            w.WriteElementString("saveLocation", "Token");
                            w.WriteElementString("label", power.Name);
                            w.WriteElementString("autoExecute", "true");
                            w.WriteElementString("sortby", GetSortPriority(power));

                            // set colors based on usage limits
                            if (GetColors(power.Usage, out text, out background))
                            {
                                w.WriteElementString("fontColorKey", text);
                                w.WriteElementString("colorKey", background);
                            }

                            // start with the table and header
                            command.AppendLine("<table border=\"0\">");
                            command.AppendFormat("<tr bgcolor=\"{0}\" style=\"color: white;\">", background);
                            command.AppendLine();
                            command.AppendFormat("<th align=\"left\" style=\"font-size: 1.1em; padding: 2px 4px;\">{0}</th>", power.Name);
                            command.AppendLine();
                            command.AppendFormat("<td align=\"right\" valign=\"middle\" style=\"padding: 2px 4px;\"><i>{0}</i></td></tr>", power.Weapon == WEAPON_NONE ? "" : power.Weapon);
                            command.AppendLine();
                            command.AppendLine("</tr>");

                            // write compendium info
                            foreach (string line in power.CompendiumInfo)
                                command.AppendLine("<tr><td colspan=\"2\">" + line + "</td></tr>");

                            // when we have a weapon, put the macro in the weapon's group and include the rolls
                            if (power.Weapon != WEAPON_NONE)
                            {
                                w.WriteElementString("group", "Attacks - " + power.Weapon);

                                command.AppendFormat("<tr><td nowrap><b>[1d20+{0}]</b></td><td><b>vs. {1}</b></td></tr>", power.AttackBonus, power.Defense);
                                command.AppendLine();
                                command.AppendFormat("<tr><td nowrap>[{0}]</td><td>damage</td></tr>", power.DamageExpression);
                                command.AppendLine();
                            }

                            // finish up the command
                            command.AppendLine("</table>");

                            // write the command and end element
                            w.WriteElementString("command", command.ToString());
                            w.WriteEndElement(); // MACRO_ELEMENT_NAME
                        }

                        // write skill check macros
                        foreach (KeyValuePair<string, int> skill in skills)
                        {
                            // begin macro definition
                            w.WriteStartElement(MACRO_ELEMENT_NAME);
                            w.WriteElementString("saveLocation", "Token");
                            w.WriteElementString("label", skill.Key);
                            w.WriteElementString("autoExecute", "true");
                            w.WriteElementString("group", "Skills");
                            w.WriteElementString("command", string.Format("<p><b>{0} Check</b> [1d20+{1}]</p>", skill.Key, skill.Value));
                            w.WriteEndElement(); // MACRO_ELEMENT_NAME
                        }

                        w.WriteEndDocument();
                    }
                }
            });

            try
            {
                d.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        private static bool IsPowerBasicAttack(string name)
        {
            return name.EndsWith("Basic Attack");
        }

        private static string GetSortPriority(PowerInfo power)
        {
            switch (power.Usage)
            {
                case PowerUsage.AtWill:
                    if (IsPowerBasicAttack(power.Name))
                        return "09";
                    else
                        return "10";
                case PowerUsage.Encounter:
                    return "20";
                case PowerUsage.Daily:
                    return "30";
                default:
                    return "99";
            }
        }

        private static bool GetColors(PowerUsage usage, out string text, out string background)
        {
            switch (usage)
            {
                case PowerUsage.AtWill:
                    text = "black";
                    background = "green";
                    return true;
                case PowerUsage.Encounter:
                    text = "white";
                    background = "red";
                    return true;
                case PowerUsage.Daily:
                    text = "white";
                    background = "black";
                    return true;
                default:
                    text = "";
                    background = "";
                    return false;
            }
        }

        protected class PowerInfo
        {
            private List<string> m_compendium_info;

            public string Name { get; set; }
            public string Weapon { get; set; }
            public PowerUsage Usage { get; set; }
            public string ActionType { get; set; }
            public int AttackBonus { get; set; }
            public string AttackStat { get; set; }
            public string DamageExpression { get; set; }
            public string Defense { get; set; }

            public List<string> CompendiumInfo
            {
                get { return m_compendium_info; }
            }

            public PowerInfo(string name, List<string> compendium_info, string weapon, PowerUsage usage, string action_type)
            {
                if (compendium_info == null)
                    throw new ArgumentNullException("compendium");

                this.Name = name;
                this.Weapon = weapon;
                this.Usage = usage;
                this.ActionType = action_type;

                m_compendium_info = compendium_info;
            }
        }

        protected enum PowerUsage
        {
            AtWill,
            Encounter,
            Daily
        }
    }
}
