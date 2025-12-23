using LTKCC.Models;
using SQLite;

namespace LTKCC.Services;

public sealed class WorkflowExecutor
{
    private readonly SQLiteAsyncConnection _db;

    public WorkflowExecutor(LTKCC.Data.AppDb appDb) => _db = appDb.Connection;

    public async Task<Guid> ExecuteScheduledTaskAsync(ScheduledTaskRow task, WorkflowRow workflow)
    {
        var run = new ScheduledTaskRunRow
        {
            Id = Guid.NewGuid(),
            ScheduledTaskId = task.Id,
            WorkflowId = task.WorkflowId,
            WorkflowVersion = task.WorkflowVersion,
            StartedUtc = DateTime.UtcNow,
            Status = "running"
        };

        await _db.InsertAsync(run);

        try
        {
            var steps = await _db.Table<WorkflowStepRow>()
                .Where(s => s.WorkflowId == workflow.Id && s.WorkflowVersion == workflow.Version && s.IsEnabled)
                .OrderBy(s => s.StepOrder)
                .ToListAsync();

            foreach (var step in steps)
            {
                var stepRun = new WorkflowStepRunRow
                {
                    Id = Guid.NewGuid(),
                    ScheduledTaskRunId = run.Id,
                    WorkflowStepId = step.Id,
                    StepOrder = step.StepOrder,
                    StartedUtc = DateTime.UtcNow,
                    Status = "running"
                };

                await _db.InsertAsync(stepRun);

                try
                {
                    // TODO: Dispatch based on step.ActionType and parse step.ActionDataJson.
                    // e.g. await ExecuteActionAsync(step.ActionType, step.ActionDataJson, task.ParametersJson);

                    stepRun.Status = "succeeded";
                    stepRun.OutputJson = "{}";
                }
                catch (Exception ex)
                {
                    stepRun.Status = "failed";
                    stepRun.Error = ex.ToString();
                    throw;
                }
                finally
                {
                    stepRun.FinishedUtc = DateTime.UtcNow;
                    await _db.UpdateAsync(stepRun);
                }
            }

            run.Status = "succeeded";
            return run.Id;
        }
        catch (Exception ex)
        {
            run.Status = "failed";
            run.Error = ex.ToString();
            return run.Id;
        }
        finally
        {
            run.FinishedUtc = DateTime.UtcNow;
            await _db.UpdateAsync(run);
        }
    }
}
