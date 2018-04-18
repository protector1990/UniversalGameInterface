# UniversalGameInterface
Main source code repository for Universal Game Interface

# What is UGI?

UGI (Universal Game Interface) is imagined to be a simple and open protocol for network communication in video games, for multiplayer and other uses, although it can be used for network communication in general.

# Why UGI?

Existing multiplay tools are usually closed-source and closed-specificaitons, so you cannot use a platform that your tool does not support. UGI is especially interesting when you need to mix up platforms, e.g. have different users run the game on different platforms. Another interesting use case would be if creating distributed game systems (like distributed flight simulators), and where some modules are not supported by a game engine or networking library you are using, with UGI you can just write your own implementation for that platform and connect it to the remainder of the system.

# How did UGI came to existance

Creating UGI was my graduation thesis on Belgrade Metropolitan University, Faculty of Information Technologies, Department for Video Games.

# UGI protocol specification

UGI protocol is described in ProtocolDescription.txt.

# Repository contents

This repository contains implementations for UGI server for Unity and UGI client for Java, in form of libraries. Also, there is a sample distributed network "game" built with UGI, which has two modules: Ball, implemented in unity, and BallController, on Android. When you connect those two, you can move the ball with WASD in unity, and the movement will be reflected on android app, and from android app you can change the colors.

# Contributing

As you can imagine, UGI is still work in progress, so it would have to be expanded (both the protocol specification and implementation libraries) in order to be considered for any serious use. If you ifnd this idea interesting and would like to contribute, you are more than welcome.
