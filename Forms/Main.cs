using System;
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

        public Main()
        {
            InitializeComponent();

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
            Dictionary<string, string> power_definitions = new Dictionary<string, string>();

            d.AddTask((progress) =>
            {
                progress.Update("Scanning powers...");

                using (FileStream fs = File.OpenRead(character_path))
                {
                    XPathDocument doc = new XPathDocument(fs);
                    XPathNavigator nav = doc.CreateNavigator(), temp_nav;
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
                        PowerUsage usage;

                        try
                        {
                            // read the basic stuff
                            name = power_iter.Current.SelectSingleNode("@name").Value;
                            usage = Util.EnumParse<PowerUsage>(power_iter.Current.SelectSingleNode("specific[@name = 'Power Usage']").Value.Replace("-", ""));
                            action_type = power_iter.Current.SelectSingleNode("specific[@name = 'Action Type']").Value.Trim();

                            // get the url for the power in the compendium and attempt to retrieve it
                            temp_nav = null; // nav.SelectSingleNode("//RulesElementTally/RulesElement[@name = \"" + name + "\"]/@url");
                            if (temp_nav != null)
                            {
                                HtmlAgilityPack.HtmlDocument scraper_doc = new HtmlAgilityPack.HtmlDocument();
                                HtmlAgilityPack.HtmlNodeNavigator scraper_result;
                                XPathNavigator scraper_nav;

                                // slurp
                                using (Stream s = m_compendium.GetEntryByUrl(temp_nav.Value))
                                {
                                    scraper_doc.Load(s);
                                    scraper_nav = scraper_doc.CreateNavigator();
                                }
                                    
                                // save the content part
                                scraper_result = scraper_nav.SelectSingleNode("//div[@id = 'detail']") as HtmlAgilityPack.HtmlNodeNavigator;
                                if (scraper_result != null)
                                    power_definitions[name] = scraper_result.CurrentNode.OuterHtml;
                            }

                            // select weapons
                            weapon_iter = power_iter.Current.Select("Weapon");

                            // some powers aren't attacks or don't use weapons
                            if (weapon_iter.Count < 1)
                            {
                                powers.Add(new PowerInfo(name, WEAPON_NONE, usage, action_type));
                            }
                            else
                            {
                                while (weapon_iter.MoveNext())
                                {
                                    PowerInfo power = new PowerInfo(name, weapon_iter.Current.SelectSingleNode("@name").Value, usage, action_type);

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

                        // wait briefly to avoid spamming compendium, then advance progress
                        //System.Threading.Thread.Sleep(200);
                        progress.Advance();
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
                    settings.Encoding = new UTF8Encoding(false);
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
                            command.AppendLine("<table border=\"0\" width=\"300\">");
                            command.AppendFormat("<tr bgcolor=\"{0}\" style=\"color: white;\">", background);
                            command.AppendLine();
                            command.AppendFormat("<th align=\"left\" style=\"font-size: 1.1em; padding: 2px 4px;\">{0}</th>", power.Name);
                            command.AppendLine();
                            command.AppendFormat("<td align=\"right\" valign=\"middle\" style=\"padding: 2px;\">{0}</td></tr>", power.ActionType);
                            command.AppendLine();
                            command.AppendLine("</tr>");

                            // when we have a weapon, put the macro in the weapon's group and include the rolls
                            if (power.Weapon != WEAPON_NONE)
                            {
                                // ...but put unarmed attacks in the default group
                                if (power.Weapon != WEAPON_UNARMED)
                                    w.WriteElementString("group", power.Weapon);

                                command.AppendLine("<tr><td colspan=\"2\"><i>" + power.Weapon + "</i></td></tr>");
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
            public string Name { get; set; }
            public string Weapon { get; set; }
            public PowerUsage Usage { get; set; }
            public string ActionType { get; set; }
            public int AttackBonus { get; set; }
            public string AttackStat { get; set; }
            public string DamageExpression { get; set; }
            public string Defense { get; set; }

            public PowerInfo(string name, string weapon, PowerUsage usage, string action_type)
            {
                this.Name = name;
                this.Weapon = weapon;
                this.Usage = usage;
                this.ActionType = action_type;
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
