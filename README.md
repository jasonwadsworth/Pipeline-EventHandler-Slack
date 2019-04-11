# Pipeline-EventHandler-Slack
CodePipeline Event Handler to post messages to Slack

Publish the project, zip it up, push to S3, and use the `deploy.template` CloudFormation template to deploy.

`dotnet publish -c Release -o ./publish`

`zip -r pipeline-eventhandler-slack.zip ./publish`

`aws s3 cp ./pipeline-eventhandler-slack.zip [S3Uri]`