using Luiu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using System.Data.Common;
using System.Diagnostics;

namespace Luiu.Controllers
{
    public class ViolationRuleController : Controller
    {
        public IActionResult List()
        {
            LuiuDbContext db = new LuiuDbContext();
            var rules = db.TViolationRules.Select(r => new CViolationRuleWrap { rule = r });


            return View(rules);
        }

        [HttpPost]
        public IActionResult Create(CViolationRuleWrap rule)
        {
            Debug.WriteLine($"[VloationRule] rule: {rule}");
            LuiuDbContext db = new LuiuDbContext();
            db.TViolationRules.Add(rule.rule);
            db.SaveChanges();
            return RedirectToAction("List");
        }

        public IActionResult Edit(int? id)
        {
            Debug.WriteLine($"[ViolationRuleController - Edit] id: {id}");
            LuiuDbContext db = new LuiuDbContext();
            TViolationRule rule = db.TViolationRules.FirstOrDefault(m => m.RuleId == id);

            if (rule == null) return RedirectToAction("List");

            CViolationRuleWrap ruleW = new CViolationRuleWrap();
            ruleW.rule = rule;
            var ruleTypes = db.TViolationRules
                  .Select(r => r.RuleType)
                  .Distinct()
                  .OrderBy(t => t == "其他" ? 1 : 0)
                  .Select(t => new SelectListItem
                  {
                      Value = t,
                      Text = t,
                      Selected = (t == rule.RuleType) // 自動選中原本的分類
                  })
                  .ToList();
            ViewBag.TypeItems = ruleTypes;
            return View(ruleW);
        }

        [HttpPost]
        public IActionResult Edit(CViolationRuleWrap uiRule)
        {
            Debug.WriteLine($"[ViolationRule] Edit: {uiRule}");
            LuiuDbContext db = new LuiuDbContext();
            TViolationRule dbRule = db.TViolationRules.FirstOrDefault(r => r.RuleId == uiRule.RuleId);
            
            if (dbRule != null)
            {
                dbRule.Title = uiRule.Title;
                dbRule.Description = uiRule.Description;
                dbRule.RuleType = uiRule.RuleType;
                dbRule.IsActivate = uiRule.IsActivate;
                dbRule.Severity = uiRule.Severity;
                dbRule.DefaultAction = uiRule.DefaultAction;
                dbRule.UpdatedAt = DateTime.Now;
                Debug.WriteLine($"[ViolationRule] Edit dbRule: {dbRule}");
                db.SaveChanges();
            }
            return RedirectToAction("List");
        }
    
        public IActionResult Delete(int? id)
        {
            LuiuDbContext db = new LuiuDbContext();
            TViolationRule x = db.TViolationRules.FirstOrDefault(r => r.RuleId == id);
            if (x != null)
            {
                db.TViolationRules.Remove(x);
                db.SaveChanges();
            }
            return RedirectToAction("List");
        }
    }
}
