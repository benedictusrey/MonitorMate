using MonitorMate.Reminders;
var failures = 0; var count = 0;
void Check(string name, Action action) { count++; try { action(); Console.WriteLine("PASS " + name); } catch(Exception e) { failures++; Console.WriteLine("FAIL " + name + ": " + e.Message); } }
void Require(bool ok) { if (!ok) throw new Exception("Unexpected reminder state."); }
ReminderEngine Started() { var engine = new ReminderEngine(); engine.Start(20,20); return engine; }
Check("Start waits a full work interval", () => { var e=Started(); Require(e.Phase==ReminderPhase.Working && e.Remaining==TimeSpan.FromMinutes(20)); });
Check("Boundary opens a break", () => { var e=Started(); e.Advance(TimeSpan.FromMinutes(20)); Require(e.Phase==ReminderPhase.Break && e.Remaining==TimeSpan.FromSeconds(20)); });
Check("Pause preserves remaining time", () => { var e=Started(); e.Advance(TimeSpan.FromMinutes(3)); e.Pause(); e.Advance(TimeSpan.FromHours(1)); Require(e.IsPaused && e.Remaining==TimeSpan.FromMinutes(17)); });
Check("Resume continues the same interval", () => { var e=Started(); e.Pause(); e.Resume(); e.Advance(TimeSpan.FromSeconds(5)); Require(!e.IsPaused && e.Remaining==TimeSpan.FromSeconds(1195)); });
Check("Skipping does not count a completed break", () => { var e=Started(); e.TakeBreak(20); e.SkipBreak(); Require(e.CompletedBreaks==0 && e.Phase==ReminderPhase.Working); });
Check("Completion counts once and begins work", () => { var e=Started(); e.TakeBreak(20); e.Advance(TimeSpan.FromSeconds(20)); Require(e.CompletedBreaks==1 && e.Phase==ReminderPhase.Working && e.Remaining==TimeSpan.FromMinutes(20)); });
Check("Delayed tick does not invent repeated completed breaks", () => { var e=Started(); e.Advance(TimeSpan.FromDays(1)); Require(e.CompletedBreaks==0 && e.Phase==ReminderPhase.Break && e.Remaining==TimeSpan.FromSeconds(20)); });
Check("Stop cannot resume a stale timer", () => { var e=Started(); e.Stop(); e.Resume(); e.Advance(TimeSpan.FromMinutes(30)); Require(e.Phase==ReminderPhase.Stopped && e.Remaining==TimeSpan.Zero); });
Check("Manual break works before starting", () => { var e=new ReminderEngine(); e.TakeBreak(20); Require(e.Phase==ReminderPhase.Break && !e.IsPaused); });
Check("Invalid configuration is rejected", () => { var e=Started(); try { e.Start(0,20); throw new Exception("Accepted zero work interval."); } catch(ArgumentOutOfRangeException) { } Require(e.Remaining==TimeSpan.FromMinutes(20)); });
Check("Negative elapsed time is rejected", () => { var e=Started(); try { e.Advance(TimeSpan.FromSeconds(-1)); throw new Exception("Accepted negative elapsed."); } catch(ArgumentOutOfRangeException) { } });
Console.WriteLine($"{count} reminder checks; {failures} failures."); return failures==0?0:1;
