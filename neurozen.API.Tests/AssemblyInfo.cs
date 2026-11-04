// Integration tests and BDD scenarios share one API instance and one MySQL database per run.
// Running test classes sequentially keeps them independent of each other's timing.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
