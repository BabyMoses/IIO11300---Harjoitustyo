   M S B A M L   `   `   `  ����  	TwitchBot    TwitchBot.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � Title$  	TwitchBot��5   	   $��354.93��6   $	��525��6+    � 
ResizeMode$ CanMinimize=�67   .��5      � ��5   
   �� 5       �� ImageSource$ /Images/carbon.jpg��6    j� Stretch$ UniformToFill=�6:   .+�5
   
   �� -   5         ��Name$ 
lblViewers��5
      $�� ��6$   $
��Left=�6/   $��
10,290,0,0q�6J   $	��Top=�6^   $	��104��6v   $��35��6�   ���	#FFE4E4E4$��Segoe UI Semilight)�6�   �� 5   
   -   5        $ lblViews��5      $�� ��6"   $
��Left=�6-   $��154,290,0,0q�6H   $	��Top=�6]   $��35��6u   ���	#FFE4E4E4$	��134��6�   $��Segoe UI Semilight)�6�   �� 5   
   -   5        $ lblFollowers��5      $�� ��6&   $
��Left=�61   $��293,290,0,0q�6L   $	��Top=�6a    J� RenderTransformOrigin$ 2.464,0.312&�6y   $��35��6�   ���	#FFE4E4E4$	��155��6�   $��Segoe UI Semilight)�6�   �� 5   
   -   5         ��Name$ txtChat��5      $
��Left=�6'   $	��285��6B   $	��Top=�6O   $	��509��6g   $
��Auto=�6s   $
��Auto=�5   
   ��. $��Segoe UI Semilight)�5   /   $��14(�6O   ���	#FF1B1B1B �� SelectionBrush �	#FF007FFF���	#FFCDCDCD J� Opacity$	 0.7Q�6�   	 � 	Resources	 5      
 �� 
TargetType �� Property �� Value��%X   'J�       �� 5      "
 J�6   .�5      ��  �@��Margin$ 0q�6/   5      5      5      d� 5   
   -   5        $��	btnToggle��5      $
��Left=�6(   $��445,285,0,0q�6C   $	��Top=�6X   $��58��6p   $��32��5   g   $��0q�6s   $��0q�6�    d� 	IsChecked 6B   �� *IsCheckedState+	 5         OnImage  OffImage��%}   &	        &	 4     �� 5      $��/Images/on.png��6(   |�. 6@   �� 5      $��/Images/off.png��6)   |�. 6B   5      ��5      �� 5      ��5"      �� 5#      "
 d�6    �� Triggers 5$      P� 5%       P� Property �@d�	IsChecked P� Value$
 TrueY�67   . �5&      ��  � ��# [  68   5'      5(      P� 5)       �@d�	IsChecked$ FalseY�67   . �5*      ��  � ��# [ 68   5+      5,      50      54      58      5:      