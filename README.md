# FactoRL-Assets

The project implements a generic simulation environment for Collaborative Multi-Robot System (CMRS) oriented smart manufacturing scenarios in Unity3D, and use reinforcement learning in virtue of [OpenAI ML-Agents](https://github.com/Unity-Technologies/ml-agents) to achieve a general approach to train collaborative AI for heterogenous agents in CMRS to optimize the production performance. 

![scenarioRay](https://user-images.githubusercontent.com/50829041/214954100-084fc5d1-f84c-4088-a711-007312f8bdd1.png)

# The simulation environment

In this project, an interactable CMRS manufacturing scenario is implemented in Unity3D. You can use this project to easily set up scenarios, train and deploy decision-making models, and see how they perform. 

#### Concepts and rules

Instead of proposing a sheer mathematic model, the operation in the manufacturing plant is abstracted as the interactions between the following entities following some basic rules, which make it more realistic and general (and much more complicated for traditional optimization approaches)

![image](https://user-images.githubusercontent.com/50829041/214954245-1690fa07-15d4-45f8-9a07-701c7cea8d4a.png)

**Materials**

The materials refers to any objects that involved in the workflow. There are types of materials, e.g., steel sheets, bolts, chips.

**Import station**

Each import station provides a set of raw materials.

**Export station**

Each export station receives a set of final products to meet orders. Final product can be deem as a special type of material that are requested by orders.

**Order**

Orders are continually posted and completed (or dropped) during the operation of the plant. Each order has a request list of final products and a due time.

**Workstation**

The workstations process, compose, or decompose materials of one or more types into other materials of one or more types. Each workstation can only perform specific operations in specific time. There are buffer areas for its input and output. The workstation can decide on which operation to perform at any time.

**Transporter**

The transporters moves materials among the workstations as well as the import/export stations. A transporter can load several units of materials, and it can decide on its movement at any time.



The goal of the whole system is to complete orders as more and as fast as possible. An overall illustration is as followed.

![image](https://user-images.githubusercontent.com/50829041/214954292-a10103b4-2427-4c2c-a657-7c2498500e3c.png)

And the following is an example of the processing workflow. This can be used to define complex manufacturing procedures.

![image](https://user-images.githubusercontent.com/50829041/214954319-15839c2f-913d-4dec-bd7c-7bf741e88936.png)

The mechanism in this simulation environment is intuitive and actually practical in real smart manufacturing systems. There are several reasons.

- The workstation and transporter correspond to robots in the plant, that observe information by their sensors and make most decisions locally.
- The job shop problem commonly applied in manufacturing scheduling deem the workflow as a linear process, which is usually not the case in real plants. In this project the workflow of the manufacturing is actually modelled as a graph, where the vertexes are the material types, and the edges are the workstation operations. 
- The manufacturing scenarios are usually more like game scenarios with rules than strict mathematical model. It is almost impossible to cover all the complicated but important factors in formulas. E.g., the surroundings in the plant, the interference between robots and machines, the people... But they are much easier to be simulated in virtual environments by lines of code.

# The RL, the AI

For a given setup of plant, the decision-making of the workstation (who decides on its operation) and the transporter (who decides on its movement) determine the performance of the system. Meanwhile, since there are many participants in the system, they have to make decisions as a team. 

So the objective is to find a nice algorithm to let the workstations and transporters make good decisions, right? Well, it turns out that it is really tough to apply traditional approaches here. Basically because

- The scenario is too complicated to be defined as a mathematical problem to solve.
- Heuristics approaches produce static solutions, while the scenario is dynamic and continually re-scheduling is neither well-performing nor efficient.
- They are centralized approaches, which means that they typically require god's-eye view of the system and produce global solutions. In practice, the system has to gather all information from robots in the environment, then it produces solution and distributes decisions to robots. The communication limits may make this infeasible.
- More desperately, the production scenario is subject to change while the approach is not. The design of these approaches are based on assumptions, and when some assumptions failed to stand in new scenario it is likely to start over. (which is also a significant barrier to the application of robots in the industry!)

Is there a way out? I think so! The reinforcement learning (RL) based approach used in this project can be a solution. Because

- RL model can be trained with simulation environment without a explicit mathematical model on the problem (refers to model-free reinforcement learning). Which is much easier to be implemented.
- RL perfectly fits dynamic scenarios since the output is a policy function that always tells robots what to do next based on their observations.
- RL is also intuitive to distributed systems since the robots are basically receiving *observations* and performing *actions*. With a policy function, each robot can make decisions locally and independently with its observation.
- More exhilaratingly, the production scenario is subject to change and the RL approach is as well. When the scenario changes (e.g., the robots will now have limited battery life, there will be workers wandering in the plant, or even the workflow is completely changed), it just needs to implement new constraints in the simulation environment, and train the RL model again, with same approach! 

Basically, those are the motivation of this project.

# Architecture

This project employs the OpenAI ML-agents to train the model. It is a powerful RL framework integrated with Unity3D. The algorithm used for the training is MA-POCA (MultiAgent POsthumous Credit Assignment), which trains a centralized critic neural network for all agents in the team to achieve collaborative behaviors. 

This project makes an improvement on the ML-agents implementation to MA-POCA. In the original implementation, the agents in a collaborative team must be homogeneous, which means that they must have identical observation spaces and action spaces. However, in this scenario, the workstations and transporters have different spaces, and the spaces of workstations or transporters of different types may also differ from others. Then it can only train agents with identical spaces separately. But in this way, the whole system will not behave cooperatively, and it also causes training divergence.

The solution in this project is to use an orthometric operator on heterogenous agents. The observation spaces and action spaces are mapped into union observation space and union action space. Therefore, the model can still be trained with a centralized network. And the trained model can be used for decentralized decision-making by mapping it back to individual spaces.

![image](https://user-images.githubusercontent.com/50829041/214954368-22a09fc6-6020-4830-b0c9-c592647def9d.png)

# Training and evaluation

There is a scene used for training and evaluation. You can import the scenario XML and start training the model with ML-agent client. Or you can import the trained model file so that you can see how the trained system is performing. Metrics will be evaluated and displayed on the side.

https://user-images.githubusercontent.com/50829041/214954539-d748a427-ddaf-4191-a84a-b8471cf07d0b.mp4

# Setup new scenarios

To make things easier, a scenario setup tool is implemented. You can use an XML file to define the workflow and entities, and use a visualized scene to set the layout of the environment, which will export the XML with all the scenario setup information.

![image](https://user-images.githubusercontent.com/50829041/214954386-78730a9a-660e-487b-8483-85a6f9ec53ea.png)

![image](https://user-images.githubusercontent.com/50829041/214954409-5309f14f-1039-4f9d-a58f-1cf09a65c6bc.png)

