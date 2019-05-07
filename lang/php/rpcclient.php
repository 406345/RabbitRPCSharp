<?php
require_once __DIR__ . '/vendor/autoload.php';
use PhpAmqpLib\Connection\AMQPStreamConnection;
use PhpAmqpLib\Message\AMQPMessage;

class RPCClient
{
    private $connection;
    private $channel;
    private $callback_queue;
    private $response;
    private $corr_id;

    private $stack = array(1=>'',2=>'');

    public function onResponse($rep)
    {
        if ($rep->get('correlation_id') == $this->corr_id) {
            $this->response = $rep->body;
        }
    }

    public function service($service)
    {
        $this->stack[1] = $service;
        return $this;
    }

    public function function($function)
    {
        $this->stack[2] = $function;
        return $this;
    }

    public function call(...$parameter)
    {
        $ret = $this->callAct(
            $service = $this->stack[1],
            $func = $this->stack[2],
            $parameter);
            
        return $ret;
    }

    private function callAct($service,$func,...$paramters)
    {
        $this->response = null;
        $this->corr_id = uniqid();

        list($this->callback_queue, ,) = $this->channel->queue_declare(
            "",
            false,
            false,
            true,
            false
        );

        $this->channel->basic_consume(
            $this->callback_queue,
            '',
            false,
            true,
            false,
            false,
            array(
                $this,
                'onResponse'
            )
        );

        $paras = array_pop($paramters); 

        $msg = new AMQPMessage(
            MongoDB\BSON\fromPHP($paras),
            array(
                'correlation_id' => $this->corr_id,
                'reply_to' => $this->callback_queue
            )
        );

        $this->channel->basic_publish($msg, $service, $func);
        try{
            while (!$this->response) {
                $this->channel->wait(null,false,5);
            }
        }
        catch(Exception $e){
            dump('errors');
            return null;
        }

        return MongoDB\BSON\toPHP($this->response);
    }

    public function __construct(
        $host           = 'default',
        $port           = 5672,
        $un             = 'default',
        $pwd            = 'default',
        $virtualHost    = '/default'
    )
    {
        $this->connection = new AMQPStreamConnection(
            $host      , 
            $port       ,
            $un         ,
            $pwd        ,
            $virtualHost,
        );
        $this->channel = $this->connection->channel(); 
    }
}