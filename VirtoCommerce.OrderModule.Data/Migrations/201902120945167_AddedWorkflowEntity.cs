namespace VirtoCommerce.OrderModule.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedWorkflowEntity : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Workflow",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Workflow = c.String(nullable: false),
                        Name = c.String(nullable: false, maxLength: 512),
                        MemberId = c.String(maxLength: 128),
                        IsActive = c.Boolean(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                        ModifiedDate = c.DateTime(),
                        CreatedBy = c.String(maxLength: 64),
                        ModifiedBy = c.String(maxLength: 64),
                    })
                .PrimaryKey(t => t.Id);
            
            AddColumn("dbo.CustomerOrder", "WorkflowId", c => c.String(maxLength: 128));
        }
        
        public override void Down()
        {
            DropColumn("dbo.CustomerOrder", "WorkflowId");
            DropTable("dbo.Workflow");
        }
    }
}
