### Integrate with Snowflake using JavaScript

InRule® currently offers the ability to convert a compatible rule application into a JavaScript® (js) file. We now support integrating that same JavaScript file into Snowflake as a function or procedure providing the ability to run rules directly in Snowflake without leaving the UI.

The Snowflake CICD helper integrates a rule application to Snowflake on catalog check-in. It first gets packaged into JavaScript so those settings are needed as well. The name of the rule application will be the name of the produced function or procedure in Snowflake. 

The configuration preoperties are referenced below.

|Connection Property | Comments
--- | ---
|**User**| The user account that is configured in Snowflake.
|**Password**| Password set for the user account logging in.
|**Account**| The full account name.
|**Host**| Specifies the hostname for your account in the following format: ACCOUNT.snowflakecomputing.com.
|**Warehouse**| The configured warehouse.
|**Database**| The database being ran against.
|**Schema**| The defined schema.
|**Type**| Controls creation of a procedure or function in Snowflake. Field should be set as procedure or function.
|**RevisionTracking**| Enable revision tracking. This may become useful if you would like the rule application revision to be brought into the function or procedure to back test against other revisions. For example, if turned on, the revision number would be attached to the end of the function or procedure name where "function_4" would run revision 4 and "function_5" would run revision 5.

````
	<add key="Snowflake.User" value="{AccountUserName}"/>
	<add key="Snowflake.Password" value="{AccountPassword}"/>
	<add key="Snowflake.Account" value="{Account}"/>
	<add key="Snowflake.Host" value="{Host}"/>
	<add key="Snowflake.Warehouse" value="{WarehouseName}"/>
	<add key="Snowflake.Database" value="{Database}"/>
	<add key="Snowflake.Schema" value="{Schema}"/>
  
	<add key="Snowflake.Type" value="Procedure"/>
	
	<add key="Snowflake.RevisionTracking" value="false"/>
````


### Running in Snowflake
To run auto rules in Snowflake:
````
CALL RuleApplicationName('RootEntityName', parse_json('{Initial Entity State}'));
````
Run an explicit rule set:
````
CALL RuleApplicationName('RootEntityName', parse_json('{Initial Entity State}'), 'Explicit Rule Set');
````

An example of how to call a rule application named "MultiplicationApp" is provided below. It runs in the context of the MultiplicationProblem entity, recieves an initial entity state of {FactorA: "3", FactorB: "5"}, then runs the explicit rule set MultiplyAfterRounding.
````
CALL MultiplicationApp('MultiplicationProblem', parse_json('{FactorA: ", FactorB: 5}'), 'MultiplyAfterRounding');
````

To verify that a stored procedure or function was successfully implemented in Snowflake use the corresponding commands below within Snowflake to show a list. 

````
show procedures;

show functions;
````
